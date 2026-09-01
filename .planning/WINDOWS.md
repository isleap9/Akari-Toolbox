---
schema_version: 1
open_count: 1
waived_count: 0
fixed_count: 0
total_count: 1
last_updated: 2026-09-01T00:27:11.046Z
---

# Broken Windows Ledger

> Cross-phase defect register. With `workflow.windows_enforce` enabled, `/gsd-ship` blocks while `open_count > 0`.
> Waive with `gsd-tools windows waive <id> "<reason>"` (reason required).
> Mark fixed with `gsd-tools windows fixed <id>`.

| id | phase | kind | file | line | description | status | reason | recorded_at | resolved_at |
|----|-------|------|------|------|-------------|--------|--------|-------------|-------------|
| 1 | 01 | unrun-verify | src/AkariToolbox.App/Services/TweakHandlers/DefenderTweakHandler.cs |  | Task 2 human-check verify (real Windows machine: Tamper Protection ON/OFF flow, SHA256 integrity gate rejection, RunOnce cleanup write) not run — no live Windows test machine available in this automated worktree execution; pinned SHA256 hashes computed via direct HTTPS download+hash rather than Get-FileHash on a locally-downloaded C:\\PostInstall copy | open |  | 2026-09-01T00:27:11.046Z |  |

````json
[
  {
    "id": 1,
    "kind": "unrun-verify",
    "phase": "01",
    "file": "src/AkariToolbox.App/Services/TweakHandlers/DefenderTweakHandler.cs",
    "line": null,
    "description": "Task 2 human-check verify (real Windows machine: Tamper Protection ON/OFF flow, SHA256 integrity gate rejection, RunOnce cleanup write) not run — no live Windows test machine available in this automated worktree execution; pinned SHA256 hashes computed via direct HTTPS download+hash rather than Get-FileHash on a locally-downloaded C:\\PostInstall copy",
    "status": "open",
    "reason": "",
    "recorded_at": "2026-09-01T00:27:11.046Z",
    "resolved_at": null
  }
]
````
