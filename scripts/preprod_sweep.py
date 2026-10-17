#!/usr/bin/env python3
"""Pre-prod API sweep: hit every swagger path with role-appropriate JWT, capture results."""
import json, os, sys, urllib.parse, time
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor, as_completed
import urllib.request, urllib.error

BASE = "http://localhost:8080"
ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "test_logs" / "preprod"
OUT.mkdir(parents=True, exist_ok=True)

SWAGGER = json.loads((Path("/tmp/swagger.json")).read_text())

def jwt(role):
    p = OUT / f"jwt_{role}.txt"
    return p.read_text().strip() if p.exists() else None

JWTS = {r: jwt(r) for r in ["admin","owner","staff","trainer","trainee"]}
_gym_file = OUT / "gym_id.txt"

def _load_demo_gym():
    gym = (os.environ.get("GYM_ID") or (_gym_file.read_text().strip() if _gym_file.exists() else "")).lower()
    if not gym:
        print("ERROR: GYM_ID env unset and test_logs/preprod/gym_id.txt missing", file=sys.stderr); sys.exit(2)
    return gym

DEMO_GYM = _load_demo_gym()

# Role priority by tag — first matching role used
ROLE_BY_TAG = {
    "Admin": ["admin"],
    "Tenancy": ["admin"],
    "Analytics": ["owner","admin"],
    "Settings": ["owner","staff","admin"],
    "Gyms": ["staff","owner","admin"],
    "Billing": ["staff","owner","admin"],
    "Scheduling": ["staff","trainer","trainee"],
    "Training": ["trainer","trainee","staff"],
    "Progress": ["trainee","trainer"],
    "Nutrition": ["trainee","trainer"],
    "Communication": ["trainee","trainer","staff"],
    "Gamification": ["trainee","staff","owner"],
    "Identity": ["trainee","trainer","staff","owner","admin"],
    "PDPL": ["trainee","admin"],
}

FAKE_UUID = "00000000-0000-0000-0000-000000000001"
_ID_PARAM_HINTS = ("userid", "memberid", "trainerid", "traineeid", "staffid")
_DATE_PARAM_NAMES = ("date","from","to","startdate","enddate","since","until")
_PARAM_TYPE_FAKES = {"integer": 1, "boolean": False}

def _lookup(table, key):
    """dict.get that tolerates unhashable keys (e.g. OpenAPI 3.1 type lists)."""
    return table.get(key) if isinstance(key, str) else None

def _is_id_param(n, fmt):
    return fmt == "uuid" or any(h in n for h in _ID_PARAM_HINTS)

def _is_date_param(n, fmt):
    return fmt in ("date-time","date") or n in _DATE_PARAM_NAMES

def _fake_by_name(n, fmt):
    if "gymid" in n: return DEMO_GYM
    if _is_id_param(n, fmt): return FAKE_UUID
    if _is_date_param(n, fmt):
        return "2026-05-10" if fmt != "date-time" else "2026-05-10T12:00:00Z"
    return None

# Sample values for path params
def fake_value(name, schema):
    n = (name or "").lower()
    schema = schema or {}
    fmt = schema.get("format","")
    by_name = _fake_by_name(n, fmt)
    if by_name is not None: return by_name
    by_type = _lookup(_PARAM_TYPE_FAKES, schema.get("type",""))
    if by_type is not None: return by_type
    if name in ("page","pageSize"): return 1
    if schema.get("enum"): return schema["enum"][0]
    return "test"

def _wants_query_param(p, nm):
    # Always include required; include page/pageSize if mentioned
    return p.get("required") or nm in ("page","pageSize")

def _apply_param(path, p, qs):
    loc = p.get("in")
    nm = p.get("name")
    v = fake_value(nm, p.get("schema", {}))
    if loc == "path":
        return path.replace("{"+nm+"}", urllib.parse.quote(str(v)))
    if loc == "query" and _wants_query_param(p, nm):
        qs.append(f"{nm}={urllib.parse.quote(str(v))}")
    return path

def build_url(path, op):
    parts = path
    qs = []
    for p in op.get("parameters", []):
        parts = _apply_param(parts, p, qs)
    url = BASE + parts
    if qs: url += "?" + "&".join(qs)
    return url

_STRING_FORMAT_FAKES = {"date-time": "2026-05-10T12:00:00Z", "date": "2026-05-10", "email": "test@example.com"}
_SCALAR_FAKES = {"integer": 1, "number": 1.0, "boolean": True}

def _fake_string(fmt):
    if fmt == "uuid": return DEMO_GYM
    return _lookup(_STRING_FORMAT_FAKES, fmt) or "test"

def _resolve_scalar(s):
    t = s.get("type")
    if t == "string": return _fake_string(s.get("format",""))
    return _lookup(_SCALAR_FAKES, t)

def _resolve_object(s, depth):
    out = {}
    req = set(s.get("required", []))
    for k, ps in (s.get("properties") or {}).items():
        if k in req or len(out) < 5:
            out[k] = _resolve(ps, depth+1)
    return out

def _resolve_shape(s, depth):
    if s.get("type") == "array":
        return [_resolve(s.get("items",{}), depth+1)]
    if s.get("enum"):
        return s["enum"][0]
    if s.get("type") == "object" or "properties" in s:
        return _resolve_object(s, depth)
    return _resolve_scalar(s)

def _resolve_ref(s, depth):
    ref = s["$ref"].split("/")[-1]
    return _resolve(SWAGGER.get("components",{}).get("schemas",{}).get(ref,{}), depth+1)

def _resolve_all_of(s, depth):
    merged = {}
    for sub in s["allOf"]:
        r = _resolve(sub, depth+1)
        if isinstance(r, dict): merged.update(r)
    return merged

# follow $ref
def _resolve(s, depth=0):
    if depth>5 or not s: return {}
    if "$ref" in s: return _resolve_ref(s, depth)
    if "allOf" in s: return _resolve_all_of(s, depth)
    variants = s.get("oneOf") or s.get("anyOf") or []
    if variants: return _resolve(variants[0], depth+1)
    return _resolve_shape(s, depth)

def sample_body(op):
    rb = op.get("requestBody")
    if not rb: return None
    required = rb.get("required", False)
    content = rb.get("content", {})
    schema = (content.get("application/json") or {}).get("schema") or {}
    out = _resolve(schema)
    if out is None and required: out = {}
    return out

def hit(method, url, token, body):
    req = urllib.request.Request(url, method=method.upper())
    if token: req.add_header("Authorization", f"Bearer {token}")
    if DEMO_GYM: req.add_header("X-Gym-Id", DEMO_GYM)
    data = None
    if body is not None and method.lower() in ("post","put","patch"):
        req.add_header("Content-Type", "application/json")
        data = json.dumps(body).encode()
    try:
        with urllib.request.urlopen(req, data=data, timeout=30) as r:
            return r.status, r.read()[:2000].decode("utf-8","replace")
    except urllib.error.HTTPError as e:
        return e.code, e.read()[:2000].decode("utf-8","replace")
    except Exception as e:
        return 0, f"ERR: {type(e).__name__}: {e}"

DESTRUCTIVE = {
    ("POST",   "/api/v1/admin/gyms/{gymId}/suspend"),
    ("POST",   "/api/v1/admin/gyms/{gymId}/reject"),
    ("POST",   "/api/v1/admin/gyms/{gymId}/approve"),
    ("POST",   "/api/v1/admin/gyms/{gymId}/reactivate"),
    ("POST",   "/api/v1/admin/deploy/{scope}/{targetId}"),
    ("DELETE", "/api/v1/identity/me"),
    ("DELETE", "/api/v1/identity/account"),
    ("DELETE", "/api/v1/admin/gyms/{gymId}"),
    ("DELETE", "/api/v1/admin/users/{userId}"),
    ("DELETE", "/api/v1/admin/plans/{planId}"),
    ("DELETE", "/api/v1/gyms/{gymId}/members/{memberId}"),
    ("DELETE", "/api/v1/gyms/{gymId}/kiosk-pin"),
    ("DELETE", "/api/v1/gyms/{gymId}"),
    ("POST",   "/api/v1/gyms/{gymId}/check-in"),
}

def _is_owner_path(path):
    return (path.endswith("/kiosk-pin")
            or "/settings/" in path
            or path == "/api/v1/gyms/{gymId}" or "/staff/create" in path)

def _is_me_path(path):
    return "/me/" in path or path.endswith("/me")

# Path-pattern overrides (more specific than tag), checked in order.
_PATH_ROLE_RULES = (
    (lambda path: "/admin/" in path, "admin"),
    (_is_me_path, "trainee"),
    (lambda path: "staff-create" in path, "owner"),
    (lambda path: path == "/api/v1/gyms", "admin"),  # RegisterGym is admin-only
    (lambda path: "/analytics/peak-hours" in path, "admin"),
    (_is_owner_path, "owner"),
)

def role_for(tags, path):
    for matches, role in _PATH_ROLE_RULES:
        if matches(path): return role
    return next((r for t in tags for r in ROLE_BY_TAG.get(t, []) if JWTS.get(r)), "trainee")

HTTP_METHODS = ("get","post","put","patch","delete")

def _collect_ops(paths):
    return [(m, p, op) for p, ops in paths.items() for m, op in ops.items() if m in HTTP_METHODS]

def _refresh_body(default):
    rt_p = OUT / "refresh_admin.txt"
    return {"refreshToken": rt_p.read_text().strip()} if rt_p.exists() else default

def _request_body(p, op):
    body = sample_body(op)
    # Real creds for unauth login/refresh so sweep doesn't 401 itself
    if p == "/api/v1/identity/login":
        return {"email": "admin@demo.local", "password": "123456"}
    if p == "/api/v1/identity/refresh-token":
        return _refresh_body(body)
    return body

def _probe(m, p, op, tok):
    url = ""
    try:
        url = build_url(p, op)
        body = _request_body(p, op)
        status, text = hit(m, url, tok, body)
    except Exception as e:
        status, text = -1, f"build err: {e}"
    return status, url, text

def _sweep_op(it):
    m, p, op = it
    tags = op.get("tags", ["untagged"])
    role = role_for(tags, p)
    row = {"method": m.upper(), "path": p, "tag": tags[0] if tags else "?", "role": role}
    if (m.upper(), p) in DESTRUCTIVE:
        return {**row, "status": "skip", "url": "", "snippet": "skipped destructive"}
    status, url, text = _probe(m, p, op, JWTS.get(role))
    return {**row, "status": status, "url": url, "snippet": text[:300]}

def _is_notable(status):
    return status != "skip" and (status in (0, 401, 403) or status >= 500)

def _print_distribution(results):
    by = {}
    for r in results:
        by[r["status"]] = by.get(r["status"], 0) + 1
    print("\n=== status distribution ===")
    for k in sorted(by, key=lambda x: -by[x]):
        print(f"  {k}: {by[k]}")

def run():
    results = []
    items = _collect_ops(SWAGGER["paths"])
    print(f"Sweeping {len(items)} ops", flush=True)
    with ThreadPoolExecutor(max_workers=8) as ex:
        for r in ex.map(_sweep_op, items):
            results.append(r)
            if _is_notable(r["status"]):
                print(f"  [{r['status']:>3}] {r['method']:5} {r['path'][:70]:70} ({r['role']})", flush=True)
    (OUT / "sweep_results.json").write_text(json.dumps(results, indent=2))
    # bucket
    _print_distribution(results)
    return results

if __name__ == "__main__":
    run()
