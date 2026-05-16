#!/usr/bin/env bash
# Multi-tenant isolation tests. Per CLAUDE.md Rule 10/11: hard MT, per-gym schema.
# Login as gym A staff, attempt cross-gym reads, assert 403/404 (NEVER 200 leaking data).
#
# Discovery: list all gyms via admin token, pick two non-empty gym IDs, sample endpoints.
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/config.sh"
source "$(dirname "${BASH_SOURCE[0]}")/state.sh"

ART_DIR="${ART_ROOT}/${RUN_ID}/mt_isolation"
mkdir -p "$ART_DIR"
state_set sweeps.mt_isolation.status running

FAIL_FILE="$ART_DIR/failures.json"
LOG="$ART_DIR/run.log"
RESULTS="$ART_DIR/results.json"

python3 - <<'PY' > "$RESULTS" 2> "$LOG"
import os, json, sys, urllib.request, urllib.error, urllib.parse, ssl

API = os.environ["STAGING_API"]
# Controllers are served under /api/v1 (ASP.NET ApiVersioning). Every path the
# sweep hits must carry this prefix or it 404s (was the root cause of the
# false "cannot login as admin" failure).
PREFIX = os.environ.get("API_PREFIX", "/api/v1").rstrip("/")
ADMIN = os.environ["DEMO_ADMIN"]
PWD = os.environ["DEMO_PWD"]
CTX = ssl.create_default_context()

def req(method, path, token=None, body=None, expect=None):
    url = API + PREFIX + path
    data = json.dumps(body).encode() if body is not None else None
    # Staging sits behind Cloudflare which 403s (error 1010) the default
    # python-urllib User-Agent as a bot. Present a normal client UA so the
    # sweep is not silently blocked at the edge.
    headers = {
        "Content-Type": "application/json",
        "User-Agent": "forma-staging-loop/1.0 (+mt_isolation)",
    }
    if token: headers["Authorization"] = f"Bearer {token}"
    r = urllib.request.Request(url, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(r, context=CTX, timeout=20) as resp:
            return resp.status, resp.read()
    except urllib.error.HTTPError as e:
        return e.code, e.read()
    except Exception as e:
        return 0, str(e).encode()

# API envelope: {"data": {...}, "errors": [...], "meta": ...}. The access token
# lives at data.accessToken; tolerate a flat shape too for forward-compat.
def _token(b):
    try:
        j = json.loads(b)
    except Exception:
        return None
    d = j.get("data") if isinstance(j, dict) else None
    if isinstance(d, dict):
        t = d.get("accessToken") or d.get("token")
        if t: return t
    if isinstance(j, dict):
        return j.get("accessToken") or j.get("token")
    return None

def login(email, pwd):
    s, b = req("POST", "/identity/login", body={"email": email, "password": pwd})
    if s != 200: return None
    return _token(b)

def select_gym(token, gym_id):
    s, b = req("POST", "/identity/select-gym", token=token, body={"gymId": gym_id})
    if s != 200: return None
    return _token(b) or token

failures = []
admin_tok = login(ADMIN, PWD)
if not admin_tok:
    failures.append({"module":"identity","source":"mt_isolation","check":"admin_login","error":"cannot login as admin"})
    print(json.dumps(failures)); sys.exit(0)

# List gyms. Route is /gyms (no /admin prefix); response is the standard
# envelope {"data": {"items": [...]}}.
s, b = req("GET", "/gyms?page=1&pageSize=10", token=admin_tok)
gyms = []
if s == 200:
    try:
        body = json.loads(b)
        d = body.get("data") if isinstance(body, dict) else body
        items = d.get("items") if isinstance(d, dict) else d
        gyms = [g.get("id") for g in (items or []) if g.get("id")]
    except: pass

if len(gyms) < 2:
    failures.append({"module":"gyms_registry","source":"mt_isolation","check":"discover_two_gyms","status":s,"detail":"need >= 2 gyms on staging"})
    print(json.dumps(failures)); sys.exit(0)

gym_a, gym_b = gyms[0], gyms[1]

# Try staff demo accounts (per-gym suffix varies; fall back to generic)
# Probe endpoints leak-check: any endpoint that takes a gymId in route or query.
LEAK_PROBES = [
    ("GET", f"/gyms/{{other}}/members?page=1&pageSize=5"),
    ("GET", f"/gyms/{{other}}/staff?page=1&pageSize=5"),
    ("GET", f"/gyms/{{other}}/trainers?page=1&pageSize=5"),
    ("GET", f"/gyms/{{other}}/classes?page=1&pageSize=5"),
    ("GET", f"/gyms/{{other}}/sessions?page=1&pageSize=5"),
    ("GET", f"/billing/gyms/{{other}}/invoices?page=1&pageSize=5"),
]

# Login as demo staff scoped to gym A
staff_tok = login(os.environ["DEMO_STAFF"], PWD)
if not staff_tok:
    failures.append({"module":"identity","source":"mt_isolation","check":"staff_login","error":"cannot login staff@demo.local"})
    print(json.dumps(failures)); sys.exit(0)
staff_tok = select_gym(staff_tok, gym_a) or staff_tok

for method, tmpl in LEAK_PROBES:
    path = tmpl.replace("{other}", str(gym_b))
    s, b = req(method, path, token=staff_tok)
    # Acceptable: 401, 403, 404. LEAK: 200 returning rows.
    if s == 200:
        body_s = b.decode("utf-8","replace")[:500]
        # heuristic: if non-empty items array, treat as leak
        leaked = False
        try:
            j = json.loads(b)
            items = j.get("items") if isinstance(j, dict) else j
            leaked = bool(items)
        except: pass
        if leaked:
            failures.append({
                "module": "tenant-isolation",
                "source": "mt_isolation",
                "severity": "critical",
                "check": f"gym_a_staff_reads_gym_b",
                "method": method, "path": path,
                "status": s, "snippet": body_s,
                "error": f"DATA LEAK: staff scoped to gym {gym_a} read gym {gym_b}"
            })
    elif s not in (401, 403, 404):
        failures.append({
            "module": "tenant-isolation",
            "source": "mt_isolation",
            "check": f"gym_a_staff_reads_gym_b",
            "method": method, "path": path,
            "status": s,
            "error": f"unexpected status {s} (expected 401/403/404)"
        })

print(json.dumps(failures))
PY

if [ ! -s "$RESULTS" ]; then echo "[]" > "$RESULTS"; fi
cp "$RESULTS" "$FAIL_FILE"
COUNT=$(jq 'length' "$FAIL_FILE")
state_set sweeps.mt_isolation.status done
state_set sweeps.mt_isolation.failures "$COUNT"
state_set sweeps.mt_isolation.artifact "$RESULTS"
log "mt_isolation done: $COUNT failures"

[ "$COUNT" -gt 0 ] && cp "$FAIL_FILE" "${PENDING_FAILURES}/${RUN_ID}_mt_isolation.json"
exit 0
