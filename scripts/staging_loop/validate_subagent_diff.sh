#!/usr/bin/env bash
# Validate a fixer subagent's branch diff against forbidden patterns + contract.
# Args: <branch> <base>
# Exit 0 = accepted; non-zero + stderr reason = reject
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/config.sh"

BRANCH="${1:?branch}"
BASE="${2:-develop}"

cd "$FORMA_ROOT"

fail() {
  echo "REJECT: $*" >&2
  exit 1
}

# 1. Branch exists + has commits
if ! git rev-parse --verify "$BRANCH" >/dev/null 2>&1; then
  fail "branch $BRANCH not found"
fi
COMMITS=$(git rev-list --count "$BASE..$BRANCH")
[ "$COMMITS" = "0" ] && fail "no commits on $BRANCH vs $BASE"

# 2. Forbidden patterns in added lines
DIFF_ADD=$(git diff "$BASE..$BRANCH" -- '*.cs' '*.ts' '*.tsx' '*.js' '*.jsx' '*.dart' 2>/dev/null | grep -E '^\+' || true)
if echo "$DIFF_ADD" | grep -qE '\[Fact\(\s*Skip|\[Theory\(\s*Skip|\[Ignore\]|@Ignore|\.only\(|\.skip\(|^\+.*xit\(|^\+.*xdescribe\('; then
  fail "skip/ignore added in diff"
fi

# 3. .gitignore / settings / secrets touched
FORBIDDEN_PATHS=$(git diff --name-only "$BASE..$BRANCH" | grep -E '^(\.gitignore|\.github/|secrets/|.*\.env|forma-389d6-firebase-adminsdk\.json)$' || true)
if [ -n "$FORBIDDEN_PATHS" ]; then
  fail "forbidden paths touched: $FORBIDDEN_PATHS"
fi

# 4. db/seed.sql Section 0 untouched (allow other edits only with explicit allow flag)
if git diff "$BASE..$BRANCH" -- db/seed.sql 2>/dev/null | grep -qE '^\+.*-- Section 0|^\-.*-- Section 0'; then
  fail "db/seed.sql Section 0 modified"
fi

# 5. Test files deleted without paired addition
DEL=$(git diff --diff-filter=D --name-only "$BASE..$BRANCH" | grep -cE '(Tests?\.cs|\.test\.|\.spec\.|_test\.dart)$' || true)
ADD=$(git diff --diff-filter=A --name-only "$BASE..$BRANCH" | grep -cE '(Tests?\.cs|\.test\.|\.spec\.|_test\.dart)$' || true)
if [ "$DEL" -gt "$ADD" ]; then
  fail "tests deleted ($DEL) > tests added ($ADD) — no replacement"
fi

# 6. Commit message contract: must include "Root cause:" and "Test added/updated:" on at least one commit
MSGS=$(git log --format=%B "$BASE..$BRANCH")
if ! echo "$MSGS" | grep -q "Root cause:"; then
  fail "no commit message contains 'Root cause:'"
fi
if ! echo "$MSGS" | grep -q "Test added/updated:"; then
  fail "no commit message contains 'Test added/updated:'"
fi

# 7. Soft check: assertion weakening (warn-only)
WEAKENED=$(git diff "$BASE..$BRANCH" | { grep -cE '^-.*Assert\.(Equal|True|False|Same|StrictEqual)' || true; })
STRENGTHENED=$(git diff "$BASE..$BRANCH" | { grep -cE '^\+.*Assert\.(Equal|True|False|Same|StrictEqual)' || true; })
if [ "$WEAKENED" -gt "$STRENGTHENED" ] && [ "$((WEAKENED - STRENGTHENED))" -gt 3 ]; then
  echo "WARN: possible assertion weakening: -$WEAKENED strong vs +$STRENGTHENED strong" >&2
fi

# 8. Edit count cap
TOUCHED=$(git diff --name-only "$BASE..$BRANCH" | wc -l | tr -d ' ')
if [ "$TOUCHED" -gt $((MAX_SUBAGENT_EDITS * 3)) ]; then  # *3 buffer for paired tests + multi-file fixes
  fail "too many files touched: $TOUCHED (cap ~$((MAX_SUBAGENT_EDITS * 3)))"
fi

echo "ACCEPT: branch=$BRANCH commits=$COMMITS files=$TOUCHED"
exit 0
