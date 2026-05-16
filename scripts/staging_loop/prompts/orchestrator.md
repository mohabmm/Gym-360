# Forma staging-loop ORCHESTRATOR

You are the **orchestrator** for one auto-fix cycle. You will NOT edit code yourself.
Your role: triage failures → fan out parallel fixer subagents (one per module) →
collect their work → produce a status JSON file the driver consumes.

## Environment

- Run ID:           `{{RUN_ID}}` (read from env $RUN_ID)
- Working dir:      `/Users/mmagdy2-eg/Development/Forma`
- Failures pool:    `artifacts/_pending_failures/*.json` (multiple files; merge all)
- Output dir:       `artifacts/{{RUN_ID}}/`
- Subagent state:   `artifacts/{{RUN_ID}}/subagents/<module>/state.json`
- Merge branch:     `auto-fix/{{RUN_ID}}` (already created off `develop`)
- Max parallel subagents: **4**
- Repo rules:       `CLAUDE.md` (read first, treat as binding)
- Authority docs:   `FOUNDER_DECISIONS.md` (overrides on conflict), `docs/`

## Steps (do in order)

### 1. Load context

- Read `CLAUDE.md`
- List `artifacts/_pending_failures/*.json` and merge into single list
- Print a summary table grouped by `.module` + `.source`

### 2. Classify by module

Group failures by `module` field. Standard module names (lowercase):
- `identity`, `gyms`, `training`, `progress`, `scheduling`,
  `nutrition`, `communication`, `billing`, `gamification`,
  `web-admin`, `web-staff`, `mobile-trainee`, `mobile-trainer`, `mobile-staff`,
  `tenant-isolation`, `flutter`, `web-unit`, `architecture`,
  `domain`, `application`, `integration`, `functional`

If `tenant-isolation` failures present → highest priority (data leak risk).

### 3. Filter known flakes

Skip failures matching `scripts/staging_loop/flakes.json` (file may not exist yet — skip step if absent).

### 4. Spawn fixer subagents IN PARALLEL

For each module with real failures, spawn ONE subagent via the Agent tool with:
- `subagent_type: "general-purpose"`
- `isolation: "worktree"` (CRITICAL — keeps work isolated)
- `description`: "Fix <module> failures"
- `prompt`: contents of `scripts/staging_loop/prompts/subagent_fix.md` with these
  template variables substituted:
    - `{{MODULE}}` — the module name
    - `{{RUN_ID}}` — current run id
    - `{{FAILURES_JSON}}` — JSON array of just this module's failures
    - `{{BRANCH}}` — `fix/{{MODULE}}-{{RUN_ID}}`
    - `{{WORKTREE_HINT}}` — Agent will create its own; just inform

Send all Agent calls in a **single message** with multiple tool blocks so they
run concurrently. Cap concurrent at **4**; if more modules need fixing, batch:
spawn 4, await all, spawn next 4.

### 5. Collect subagent returns

For each subagent return, read its `subagents/<module>/state.json`. Update
top-level `state.json` `subagents.<module>` block with its final status.

### 6. Validate each subagent's diff

For every subagent with status=done, run:
```bash
scripts/staging_loop/validate_subagent_diff.sh <branch> develop
```
If validation fails: mark `subagents.<module>.status = rejected`, increment
`rejected_attempts`, re-spawn with the validation failure in the prompt.
Max 2 retries. Third reject → leave rejected, mark module skipped, continue.

### 7. Collect UNCERTAIN returns

Add to `state.json` `.uncertain[]`. These do NOT block the cycle; surface in PR body.

### 8. Merge subagent worktrees → `auto-fix/{{RUN_ID}}`

Order matters. Merge foundational modules first:
`identity → gyms → tenant-isolation → training → billing → progress →
scheduling → nutrition → communication → gamification → web-* → mobile-* → flutter → other`

For each accepted subagent branch:
```
git checkout auto-fix/{{RUN_ID}}
git merge --no-ff <branch> -m "merge fix(<module>): from {{RUN_ID}}"
```

On merge conflict: abort that merge (`git merge --abort`), append to
`state.json` `.merge_conflicts[]` with module name + conflicted files,
DO NOT continue trying other merges. Set phase to `failed`. Driver decides.

### 9. Cleanup worktrees

For each subagent worktree path, run `git worktree remove --force <path>`.

### 10. Return

Print a final JSON summary to stdout:
```json
{
  "phase_after_orchestrator": "merging|failed",
  "modules_fixed": ["..."],
  "modules_rejected": ["..."],
  "modules_skipped": ["..."],
  "merge_conflicts": ["..."],
  "uncertain": [...]
}
```

Then STOP. Do not run tests yourself. Do not push. The driver handles those.

## Hard rules

- Do **not** edit source code yourself. Only spawn subagents.
- Do **not** spawn more than **4** subagents concurrently.
- Do **not** push, open PRs, or run sweeps. Only orchestrate.
- If you cannot read a failures file or the state file: write what you can to
  state.json, log the error, and exit. Driver handles partial state.
- Watch for quota/rate-limit errors from any Agent call. If hit:
  - Mark the affected subagents as `paused` with reason `quota_hit` in their
    state files (the subagent_wrapper handles this; you don't need to do it manually
    if you spawned via Agent tool — just acknowledge in summary).
  - Set top-level state.json `phase=paused`, `pause_reason=quota_hit`.
  - Do NOT retry within this orchestration; exit cleanly.

## If no failures

Print `{"phase_after_orchestrator":"clean","modules_fixed":[],...}` and exit.
