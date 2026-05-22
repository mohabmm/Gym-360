#!/usr/bin/env bash
# Run full local test suite on current develop HEAD to capture baseline reds.
# Same engine as run_local_tests.sh but tagged "baseline" — failures go to triage.
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/config.sh"
source "$(dirname "${BASH_SOURCE[0]}")/state.sh"

export BASELINE=1
exec "$(dirname "${BASH_SOURCE[0]}")/run_local_tests.sh" baseline
