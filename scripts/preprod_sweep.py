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
DEMO_GYM = (os.environ.get("GYM_ID") or (_gym_file.read_text().strip() if _gym_file.exists() else "")).lower()
if not DEMO_GYM:
    print("ERROR: GYM_ID env unset and test_logs/preprod/gym_id.txt missing", file=sys.stderr); sys.exit(2)

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

# Sample values for path params
def fake_value(name, schema):
    n = (name or "").lower()
    fmt = (schema or {}).get("format","")
    typ = (schema or {}).get("type","")
    if "gymid" in n: return DEMO_GYM
    if "userid" in n or "memberid" in n or "trainerid" in n or "traineeid" in n or "staffid" in n: return "00000000-0000-0000-0000-000000000001"
    if fmt == "uuid": return "00000000-0000-0000-0000-000000000001"
    if fmt in ("date-time","date") or n in ("date","from","to","startdate","enddate","since","until"):
        return "2026-05-10" if fmt != "date-time" else "2026-05-10T12:00:00Z"
    if typ == "integer": return 1
    if typ == "boolean": return False
    if name in ("page","pageSize"): return 1
    if (schema or {}).get("enum"): return (schema or {})["enum"][0]
    return "test"

def build_url(path, op):
    parts = path
    qs = []
    for p in op.get("parameters", []):
        loc = p.get("in")
        nm = p.get("name")
        sch = p.get("schema", {})
        v = fake_value(nm, sch)
        if loc == "path":
            parts = parts.replace("{"+nm+"}", urllib.parse.quote(str(v)))
        elif loc == "query":
            # Always include required; include page/pageSize if mentioned
            if p.get("required") or nm in ("page","pageSize"):
                qs.append(f"{nm}={urllib.parse.quote(str(v))}")
    url = BASE + parts
    if qs: url += "?" + "&".join(qs)
    return url

def sample_body(op):
    rb = op.get("requestBody")
    if not rb: return None
    required = rb.get("required", False)
    content = rb.get("content", {})
    schema = (content.get("application/json") or {}).get("schema") or {}
    # follow $ref
    def resolve(s, depth=0):
        if depth>5: return {}
        if not s: return {}
        if "$ref" in s:
            ref = s["$ref"].split("/")[-1]
            return resolve(SWAGGER.get("components",{}).get("schemas",{}).get(ref,{}), depth+1)
        if "allOf" in s:
            merged = {}
            for sub in s["allOf"]:
                r = resolve(sub, depth+1)
                if isinstance(r, dict): merged.update(r)
            return merged
        if "oneOf" in s or "anyOf" in s:
            for sub in (s.get("oneOf") or s.get("anyOf") or []):
                return resolve(sub, depth+1)
        if s.get("type") == "array":
            return [resolve(s.get("items",{}), depth+1)]
        if s.get("enum"):
            return s["enum"][0]
        if s.get("type") == "object" or "properties" in s:
            out = {}
            req = set(s.get("required", []))
            for k, ps in (s.get("properties") or {}).items():
                if k in req or len(out) < 5:
                    out[k] = resolve(ps, depth+1)
            return out
        t = s.get("type")
        fmt = s.get("format","")
        if t == "string":
            if fmt == "uuid": return DEMO_GYM
            if fmt == "date-time": return "2026-05-10T12:00:00Z"
            if fmt == "date": return "2026-05-10"
            if fmt == "email": return "test@example.com"
            return "test"
        if t == "integer": return 1
        if t == "number": return 1.0
        if t == "boolean": return True
        return None
    out = resolve(schema)
    if out is None and required: out = {}
    return out

def hit(method, url, token, body, gym_header=None):
    req = urllib.request.Request(url, method=method.upper())
    if token: req.add_header("Authorization", f"Bearer {token}")
    if gym_header: req.add_header("X-Gym-Id", gym_header)
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

def role_for(tags, path):
    if "/admin/" in path: return "admin"
    # path-pattern overrides (more specific than tag)
    if "/me/" in path or path.endswith("/me"): return "trainee"
    if "staff-create" in path: return "owner"
    if path == "/api/v1/gyms": return "admin"  # RegisterGym is admin-only
    if "/analytics/peak-hours" in path: return "admin"
    if path.endswith("/kiosk-pin") \
       or "/settings/" in path \
       or path == "/api/v1/gyms/{gymId}" or "/staff/create" in path:
        return "owner"
    for t in tags:
        for r in ROLE_BY_TAG.get(t, []):
            if JWTS.get(r): return r
    return "trainee"

def run():
    results = []
    paths = SWAGGER["paths"]
    items = []
    for p, ops in paths.items():
        for m, op in ops.items():
            if m not in ("get","post","put","patch","delete"): continue
            items.append((m, p, op))
    print(f"Sweeping {len(items)} ops", flush=True)
    def task(it):
        m, p, op = it
        tags = op.get("tags", ["untagged"])
        role = role_for(tags, p)
        tok = JWTS.get(role)
        if (m.upper(), p) in DESTRUCTIVE:
            return {"method": m.upper(), "path": p, "tag": tags[0] if tags else "?", "role": role, "status": "skip", "url": "", "snippet": "skipped destructive"}
        try:
            url = build_url(p, op)
            body = sample_body(op)
            # Real creds for unauth login/refresh so sweep doesn't 401 itself
            if p == "/api/v1/identity/login":
                body = {"email": "admin@demo.local", "password": "123456"}
            elif p == "/api/v1/identity/refresh-token":
                rt_p = OUT / "refresh_admin.txt"
                if rt_p.exists():
                    body = {"refreshToken": rt_p.read_text().strip()}
            status, text = hit(m, url, tok, body, gym_header=DEMO_GYM)
        except Exception as e:
            status, text = -1, f"build err: {e}"
        return {"method": m.upper(), "path": p, "tag": tags[0] if tags else "?", "role": role, "status": status, "url": url if 'url' in dir() else "", "snippet": text[:300]}
    with ThreadPoolExecutor(max_workers=8) as ex:
        for r in ex.map(task, items):
            results.append(r)
            if r["status"] == "skip": continue
            if r["status"] == 0 or r["status"] >= 500 or r["status"] == 401 or r["status"] == 403:
                print(f"  [{r['status']:>3}] {r['method']:5} {r['path'][:70]:70} ({r['role']})", flush=True)
    (OUT / "sweep_results.json").write_text(json.dumps(results, indent=2))
    # bucket
    by = {}
    for r in results:
        by.setdefault(r["status"], 0)
        by[r["status"]] += 1
    print("\n=== status distribution ===")
    for k in sorted(by, key=lambda x: -by[x]):
        print(f"  {k}: {by[k]}")
    return results

if __name__ == "__main__":
    run()
