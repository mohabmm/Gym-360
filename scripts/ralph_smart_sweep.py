#!/usr/bin/env python3
"""Smart sweep — uses real discovered IDs for path params, role-correct JWTs, realistic bodies.
Goal: max 2xx across all 5 roles; surface real 5xx/auth bugs."""
import json, os, sys, urllib.parse, time
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
import urllib.request, urllib.error

BASE = "http://localhost:8080"
ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "test_logs" / "preprod"

SWAGGER = json.loads(Path("/tmp/swagger.json").read_text())
GYM = (OUT / "gym_id.txt").read_text().strip()
JWTS = {r: (OUT / f"jwt_{r}.txt").read_text().strip() for r in ["admin","owner","staff","trainer","trainee"]}

def call(method, path, role="admin", body=None, query=None):
    url = BASE + path
    if query:
        url += "?" + urllib.parse.urlencode(query)
    req = urllib.request.Request(url, method=method.upper())
    req.add_header("Authorization", f"Bearer {JWTS[role]}")
    data = None
    if body is not None and method.lower() in ("post","put","patch"):
        req.add_header("Content-Type", "application/json")
        data = json.dumps(body).encode()
    try:
        with urllib.request.urlopen(req, data=data, timeout=30) as r:
            txt = r.read().decode("utf-8","replace")
            try: return r.status, json.loads(txt)
            except: return r.status, txt
    except urllib.error.HTTPError as e:
        txt = e.read().decode("utf-8","replace")
        try: return e.code, json.loads(txt)
        except: return e.code, txt
    except Exception as e:
        return 0, str(e)

def discover():
    """Walk list endpoints to find real IDs for the test gym."""
    ids = {"gymId": GYM}
    # members
    s, b = call("GET", f"/api/v1/gyms/{GYM}/members", "owner", query={"page":1,"pageSize":5})
    if s == 200:
        items = (b.get("data") or {}).get("items") or []
        if items:
            ids["memberId"] = items[0].get("memberId") or items[0].get("id")
            ids["userId"] = items[0].get("userId") or ids["memberId"]
            ids["traineeId"] = ids["memberId"]
    # trainers
    s, b = call("GET", f"/api/v1/gyms/{GYM}/trainers", "owner")
    if s == 200:
        items = b.get("data") or []
        if items:
            ids["trainerId"] = items[0].get("trainerId") or items[0].get("id")
    # staff
    s, b = call("GET", f"/api/v1/gyms/{GYM}/staff", "owner")
    if s == 200:
        items = b.get("data") or []
        if items:
            ids["staffId"] = items[0].get("id") or items[0].get("staffId")
    # classes
    s, b = call("GET", f"/api/v1/gyms/{GYM}/classes", "owner")
    if s == 200:
        items = b.get("data") or []
        if items:
            ids["classId"] = items[0].get("id")
    # exercises (training)
    s, b = call("GET", f"/api/v1/training/exercises", "trainer", query={"page":1,"pageSize":5})
    if s == 200:
        d = b.get("data")
        items = d.get("items") if isinstance(d, dict) else d
        if items:
            ids["exerciseId"] = items[0].get("id") or items[0].get("exerciseId")
    # owner agg
    s, b = call("GET", "/api/v1/identity/me", "owner")
    if s == 200:
        d = b.get("data") or {}
        if d.get("ownerId"): ids["ownerId"] = d["ownerId"]
    return ids

def role_for(tags, path, method):
    if "/admin/" in path: return "admin"
    if "/me/" in path or path.endswith("/me"): return "trainee"
    if path == "/api/v1/gyms" and method.lower() == "post": return "admin"
    if "/analytics/peak-hours" in path: return "owner"
    if "/identity/users/staff-create" in path: return "owner"
    if path.endswith("/kiosk-pin") or "/settings/" in path \
       or "/staff/create" in path or "/feature-overrides" in path:
        return "owner"
    if path == "/api/v1/gyms/{gymId}": return "owner"
    if "/training/" in path: return "trainer"
    if "/nutrition/" in path or "/progress/" in path: return "trainee"
    if "/scheduling/" in path: return "trainer"
    if "/owners/" in path: return "owner"
    if "/gamification/" in path: return "trainee"
    if "/communication/" in path: return "trainee"
    if "/billing/me/" in path: return "trainee"
    if "/billing/" in path: return "owner"
    if "/pdpl/" in path: return "trainee"
    if "/gyms/" in path: return "owner"
    if "/identity/" in path: return "trainee"
    return "trainee"

DESTRUCTIVE = {
    ("POST","/api/v1/admin/gyms/{gymId}/suspend"),
    ("POST","/api/v1/admin/gyms/{gymId}/reject"),
    ("POST","/api/v1/admin/gyms/{gymId}/approve"),
    ("POST","/api/v1/admin/gyms/{gymId}/reactivate"),
    ("POST","/api/v1/admin/deploy/{scope}/{targetId}"),
    ("DELETE","/api/v1/identity/me"),
    ("DELETE","/api/v1/identity/account"),
    ("DELETE","/api/v1/admin/gyms/{gymId}"),
    ("DELETE","/api/v1/admin/users/{userId}"),
    ("DELETE","/api/v1/admin/plans/{planId}"),
    ("DELETE","/api/v1/gyms/{gymId}/members/{memberId}"),
    ("DELETE","/api/v1/gyms/{gymId}/kiosk-pin"),
    ("DELETE","/api/v1/gyms/{gymId}"),
    ("POST","/api/v1/gyms/{gymId}/check-in"),
    ("POST","/api/v1/gyms/{gymId}/members/{memberId}/freeze"),
    ("POST","/api/v1/gyms/{gymId}/members/{memberId}/unfreeze"),
    ("DELETE","/api/v1/identity/refresh-token"),
}

def build_url(path, op, ids):
    parts = path
    qs = []
    for p in op.get("parameters", []):
        loc = p.get("in"); nm = p.get("name"); sch = p.get("schema", {})
        v = ids.get(nm)
        if v is None:
            n = (nm or "").lower()
            if "gymid" in n: v = ids.get("gymId", GYM)
            elif n.endswith("id"):
                # try matching id type
                key = nm[0].lower() + nm[1:]
                v = ids.get(key) or ids.get("memberId") or ids.get("userId") or "00000000-0000-0000-0000-000000000001"
            elif sch.get("type") == "integer": v = 1
            elif sch.get("type") == "boolean": v = "false"
            elif sch.get("format") in ("date-time",): v = "2026-05-12T12:00:00Z"
            elif sch.get("format") == "date" or n in ("date","from","to","since","until"):
                v = "2026-05-12" if sch.get("format") != "date-time" else "2026-05-12T12:00:00Z"
            elif sch.get("enum"): v = sch["enum"][0]
            else: v = "test"
        if loc == "path":
            parts = parts.replace("{"+nm+"}", urllib.parse.quote(str(v)))
        elif loc == "query" and (p.get("required") or nm in ("page","pageSize")):
            qs.append(f"{nm}={urllib.parse.quote(str(v))}")
    url = BASE + parts
    if qs: url += "?" + "&".join(qs)
    return url

def sample_body(op, ids):
    rb = op.get("requestBody")
    if not rb: return None
    schema = ((rb.get("content") or {}).get("application/json") or {}).get("schema") or {}
    def resolve(s, depth=0):
        if depth>6 or not s: return {}
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
        if s.get("enum"): return s["enum"][0]
        if s.get("type") == "object" or "properties" in s:
            out = {}
            for k, ps in (s.get("properties") or {}).items():
                kl = k.lower()
                # Prefer real id if available
                if kl in ("gymid",): out[k] = ids.get("gymId", GYM)
                elif kl == "memberid" and ids.get("memberId"): out[k] = ids["memberId"]
                elif kl == "userid" and ids.get("userId"):     out[k] = ids["userId"]
                elif kl == "trainerid" and ids.get("trainerId"): out[k] = ids["trainerId"]
                elif kl == "traineeid" and ids.get("traineeId"): out[k] = ids["traineeId"]
                elif kl == "staffid" and ids.get("staffId"):     out[k] = ids["staffId"]
                elif kl == "exerciseid" and ids.get("exerciseId"): out[k] = ids["exerciseId"]
                else:
                    out[k] = resolve(ps, depth+1)
            return out
        t = s.get("type"); fmt = s.get("format","")
        if t == "string":
            if fmt == "uuid": return ids.get("gymId", GYM)
            if fmt == "date-time": return "2026-05-12T12:00:00Z"
            if fmt == "date": return "2026-05-12"
            if fmt == "email": return f"sweep-{int(time.time())}@test.local"
            return "test"
        if t == "integer": return 1
        if t == "number": return 1.0
        if t == "boolean": return True
        return None
    out = resolve(schema)
    return out if out is not None else {}

def run():
    ids = discover()
    print(f"discovered ids: {json.dumps({k:v for k,v in ids.items() if k!='gymId'})}")
    results = []
    items = []
    for p, ops in SWAGGER["paths"].items():
        for m, op in ops.items():
            if m in ("get","post","put","patch","delete"):
                items.append((m, p, op))
    def task(it):
        m, p, op = it
        tags = op.get("tags", ["?"])
        role = role_for(tags, p, m)
        if (m.upper(), p) in DESTRUCTIVE:
            return {"method": m.upper(),"path": p,"role": role,"status":"skip","snippet":"destructive"}
        if p == "/api/v1/identity/login":
            body = {"email":"admin@demo.local","password":"123456"}
            url = BASE + p
        elif p == "/api/v1/identity/refresh-token":
            rt = (OUT/"refresh_admin.txt")
            body = {"refreshToken": rt.read_text().strip()} if rt.exists() else {}
            url = BASE + p
        else:
            url = build_url(p, op, ids)
            body = sample_body(op, ids)
        try:
            req = urllib.request.Request(url, method=m.upper())
            req.add_header("Authorization", f"Bearer {JWTS[role]}")
            req.add_header("X-Gym-Id", GYM)
            data = None
            if body is not None and m.lower() in ("post","put","patch"):
                req.add_header("Content-Type","application/json")
                data = json.dumps(body).encode()
            with urllib.request.urlopen(req, data=data, timeout=30) as r:
                txt = r.read().decode("utf-8","replace")
                return {"method":m.upper(),"path":p,"role":role,"status":r.status,"snippet":txt[:200]}
        except urllib.error.HTTPError as e:
            return {"method":m.upper(),"path":p,"role":role,"status":e.code,"snippet":e.read().decode("utf-8","replace")[:300]}
        except Exception as e:
            return {"method":m.upper(),"path":p,"role":role,"status":0,"snippet":f"{type(e).__name__}: {e}"}

    with ThreadPoolExecutor(max_workers=8) as ex:
        for r in ex.map(task, items):
            results.append(r)
            if r["status"] != "skip" and (r["status"] == 0 or r["status"] >= 500 or r["status"] in (401,403)):
                print(f"  [{r['status']:>3}] {r['method']:5} {r['path'][:65]:65} ({r['role']:7}) {r['snippet'][:120]}")
    (OUT/"smart_sweep_results.json").write_text(json.dumps(results, indent=2))
    by = {}
    for r in results: by[r["status"]] = by.get(r["status"],0)+1
    print("=== distribution ===")
    for k in sorted(by, key=lambda x:-by[x]): print(f"  {k}: {by[k]}")
    return results

if __name__=="__main__": run()
