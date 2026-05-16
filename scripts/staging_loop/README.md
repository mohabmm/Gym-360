# Forma staging-loop

Autonomous staging test + auto-fix + PR system for Forma.

## What it does

Three times a day (19:00 / 00:00 / 05:00 Cairo) + on demand:

1. **Health probe** — verify staging-api / admin / staff are up. Down → skip cycle.
2. **Deploy sync** — wait until staging matches `develop` HEAD SHA.
3. **Sweeps (parallel)**
   - BE API multi-role sweep (`ralph_smart_sweep.py`)
   - Web E2E (Playwright admin + staff)
   - Mobile (MobAI / Maestro) for trainee + trainer + staff Flutter apps
   - Multi-tenant isolation probes (gym A staff cannot read gym B)
   - Seq log scan for ERROR / FATAL during window
4. **Baseline local tests** — `dotnet test` for arch/domain/app/integration/functional + frontend.
   Failures here = pre-existing red on `develop`, also triaged.
5. **Triage** — merge all failures into one list, group by module.
6. **Auto-fix (multi-agent)** — orchestrator (headless `claude -p`) fans out subagents
   via the `Agent` tool, one per module, each in its own git worktree (isolated).
   Each subagent diagnoses root cause (prod bug vs test bug), fixes, writes paired test.
7. **Diff validation** — each subagent branch checked against forbidden patterns
   (no `Skip`, no test deletion w/o replacement, no `.gitignore` edits, etc.).
   Rejected → re-spawned (max 2 retries).
8. **Merge** — accepted branches merged into `auto-fix/<run_id>` in priority order.
9. **Local tests gate** — full suite must be green before push.
10. **Verify** — re-run MT isolation probes against staging.
11. **Push + PR draft** — opens draft PR on `develop`. You review + merge.

A **daily mutation test** (Stryker.NET, 08:00 Cairo) scores test quality; surviving
mutants get fed into the next cycle as "test gap" failures — subagents write tests
to kill them. Mutation score < 50% blocks PRs until addressed.

## Free + autonomous

- No paid API. Uses Claude Code Max plan via headless `claude -p`.
- Multi-agent via native `Agent` tool — single session budget, parallel workers.
- Quota hit mid-cycle → state saved w/ `paused_at` + `resume_at`. Watcher
  (every 60s) auto-resumes within a minute of reset.
- Mac wakes from sleep via launchd `WakeFromSleep`. `caffeinate` keeps it
  awake during cycle.

## State + resumability

Every cycle has `artifacts/<run_id>/state.json` written after each phase
transition + per-subagent `subagents/<module>/state.json`.

- Cycle interrupted → resume from current phase next launch.
- Subagent paused → resume w/ its prior diff + state in prompt.
- Driver lock (`/tmp/forma_staging_loop.lock` + PID) ensures NO overlapping
  cycles. Scheduled fire while previous still running → exits cleanly.

## Tests + fakes prevention

- **Test mandate**: every code fix in a subagent MUST include paired test
  (covered → passes, removed fix → fails).
- **Forbidden**: deleting tests, `Skip`/`Ignore`, weakening assertions,
  catching+swallowing exceptions.
- **Diff validation gate**: rejects any subagent that violates.
- **Mutation testing daily**: catches tests that pass but don't actually
  assert (kill-rate < threshold).

## Files

```
scripts/staging_loop/
├── config.sh                     # All knobs, URLs, timeouts
├── state.sh                      # Atomic state.json helpers
├── lock.sh                       # flock + PID guard
├── notify.sh                     # Mac desktop notify
├── resume.sh                     # Phase dispatcher
├── watcher.sh                    # 60s quota-reset poll
├── staging_loop.sh               # Main driver (phase state machine)
├── setup.sh                      # One-shot installer / unloader
├── run_health_probe.sh
├── wait_for_deploy.sh
├── run_api_sweep.sh
├── run_web_e2e.sh
├── run_mobile_mobai.sh
├── run_mt_isolation.sh
├── run_log_scan.sh
├── run_baseline_tests.sh
├── run_local_tests.sh
├── run_mutation.sh
├── mutants_to_failures.sh
├── subagent_wrapper.sh
├── validate_subagent_diff.sh
├── prompts/
│   ├── orchestrator.md
│   └── subagent_fix.md
└── launchd/
    ├── com.forma.staging-loop.plist
    ├── com.forma.staging-loop-watcher.plist
    └── com.forma.mutation-test.plist
```

## Install

```bash
cd ~/Development/Forma
./scripts/staging_loop/setup.sh
```

This:
- Checks deps (jq, python3, dotnet, gh, docker, flock, osascript, maestro)
- Creates `develop` branch if missing
- Verifies `gh auth status`
- Installs `dotnet-stryker` globally if missing
- Installs Playwright chromium
- Symlinks plists into `~/Library/LaunchAgents/`
- Loads all three jobs

## Manual trigger

```bash
./scripts/staging_loop/staging_loop.sh --manual
```

## Operate

```bash
# Tail driver log
tail -f test_logs/staging_loop/loop.log

# See active cycle state
cat artifacts/$(readlink artifacts/_active_run_id)/state.json | jq

# Active subagents
ls artifacts/$(readlink artifacts/_active_run_id)/subagents/

# Force-end a stuck cycle
rm artifacts/_active_run_id /tmp/forma_staging_loop.lock /tmp/forma_staging_loop.pid

# Unload everything
./scripts/staging_loop/setup.sh unload
```

## Optional config

Edit `config.sh`:
- `STAGING_API`, `STAGING_ADMIN`, `STAGING_STAFF` — staging URLs
- `STAGING_SEQ_URL`, `STAGING_SEQ_APIKEY` — set in env for log scan to activate
- `MAX_PARALLEL_SUBAGENTS` — concurrency cap (default 4)
- `ENABLE_*` — toggle phases on/off without code change
- `PERF_GATE_ENABLED` — flip to 1 after 30-day baseline gathered

## Cron schedule (Cairo)

| Time | Job |
|---|---|
| 00:00 | Staging cycle |
| 05:00 | Staging cycle |
| 08:00 | Mutation testing |
| 19:00 | Staging cycle |
| every 60s | Watcher (resume paused cycles) |

UTC equivalents in plists: 16:00 / 21:00 / 02:00 + 05:00 mutation. Adjust for DST if needed.
