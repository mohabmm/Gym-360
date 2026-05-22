#!/usr/bin/env bash
# Convert Stryker mutation reports → failures.json in _pending_failures pool.
# Arg: <mutation_run_dir>
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/config.sh"

DIR="${1:?usage: $0 <mutation_run_dir>}"
OUT="${PENDING_FAILURES}/$(basename "$DIR")_mutation.json"

# Collect surviving mutants from all project reports
python3 - "$DIR" <<'PY' > "$OUT"
import sys, json, glob, os
from pathlib import Path

root = sys.argv[1]
failures = []
for rpt in glob.glob(f"{root}/*/mutation-report.json"):
    proj = Path(rpt).parent.name
    try:
        data = json.load(open(rpt))
    except Exception as e:
        failures.append({"module":"unknown","source":"mutation","error":f"parse {rpt}: {e}"})
        continue
    for fpath, fdata in (data.get("files") or {}).items():
        # Infer module from path: src/Modules/<Name>/...
        parts = fpath.replace("\\","/").split("/")
        module = "unknown"
        if "Modules" in parts:
            i = parts.index("Modules")
            if i+1 < len(parts):
                module = parts[i+1].lower()
        for m in (fdata.get("mutants") or []):
            if m.get("status") != "Survived":
                continue
            loc = m.get("location") or {}
            failures.append({
                "module": module,
                "source": "mutation",
                "severity": "medium",
                "project": proj,
                "file": fpath,
                "line": (loc.get("start") or {}).get("line"),
                "mutator": m.get("mutatorName"),
                "original": (m.get("replacement") or "")[:200],
                "test_gap": f"surviving mutant: {m.get('mutatorName')} at {fpath}:{(loc.get('start') or {}).get('line')}",
                "error": f"test suite did not kill mutation {m.get('mutatorName')} — add boundary/edge test"
            })

print(json.dumps(failures))
PY

COUNT=$(jq 'length' "$OUT" 2>/dev/null || echo 0)
echo "mutants_to_failures: wrote $COUNT failures to $OUT"
[ "$COUNT" = "0" ] && rm -f "$OUT"
exit 0
