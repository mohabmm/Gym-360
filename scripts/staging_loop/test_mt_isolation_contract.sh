#!/usr/bin/env bash
# Regression test for the mt_isolation harness contract.
#
# Reproduces (red, pre-fix) and guards (green, post-fix) the bug that surfaced
# as a false "identity / admin_login: cannot login as admin" failure on the
# 20260516-130005 staging loop. Root cause was FOUR harness defects, none in
# Identity prod code:
#   1. API paths missing the /api/v1 route prefix  -> 404
#   2. DEMO_ADMIN=admin@gym-365.com (no such user) -> 401
#   3. login()/select_gym() parsed flat token, but API wraps it in {"data":...}
#   4. default python-urllib User-Agent 403'd by Cloudflare (error 1010)
#
# Static assertions only -> deterministic, no live-staging / network dependency.
set -euo pipefail
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CONFIG="$HERE/config.sh"
SCRIPT="$HERE/run_mt_isolation.sh"
fail=0
chk() { if eval "$2"; then echo "PASS: $1"; else echo "FAIL: $1"; fail=1; fi; }

# Bug 2: admin credential must be a seeded account (IdentityDevSeeder seeds
# admin@demo.local, never admin@gym-365.com).
chk "DEMO_ADMIN is admin@demo.local" \
    'grep -qE "^export DEMO_ADMIN=\"admin@demo\.local\"" "$CONFIG"'
chk "DEMO_ADMIN is NOT the bogus admin@gym-365.com" \
    '! grep -qE "DEMO_ADMIN=\"admin@gym-365" "$CONFIG"'

# Bug 1: a reusable API prefix var exists and the script honours it on every
# request by building url = API + PREFIX + path.
chk "config.sh exports API_PREFIX defaulting to /api/v1" \
    'grep -qE "API_PREFIX=.*/api/v1" "$CONFIG"'
chk "req() prepends PREFIX to every path" \
    'grep -qE "url = API \+ PREFIX \+ path" "$SCRIPT"'
chk "no un-prefixed bare /identity/login literal call slips through" \
    '! grep -qE "API \+ \"/identity" "$SCRIPT"'

# Bug 3: token extraction must read the data envelope.
chk "token parser reads data.accessToken" \
    'grep -qE "\.get\(\"data\"\)" "$SCRIPT" && grep -qE "accessToken" "$SCRIPT"'

# Bug 4: a non-default User-Agent is sent so Cloudflare does not 1010-block.
chk "req() sets a custom User-Agent header" \
    'grep -qE "\"User-Agent\":" "$SCRIPT"'

if [ "$fail" -ne 0 ]; then echo "CONTRACT BROKEN"; exit 1; fi
echo "ALL CONTRACT CHECKS PASSED"
