---
schema_version: 1
open_count: 4
waived_count: 0
fixed_count: 0
total_count: 4
last_updated: 2026-09-01T11:14:22.994Z
---

# Broken Windows Ledger

> Cross-phase defect register. With `workflow.windows_enforce` enabled, `/gsd-ship` blocks while `open_count > 0`.
> Waive with `gsd-tools windows waive <id> "<reason>"` (reason required).
> Mark fixed with `gsd-tools windows fixed <id>`.

| id | phase | kind | file | line | description | status | reason | recorded_at | resolved_at |
|----|-------|------|------|------|-------------|--------|--------|-------------|-------------|
| 1 | 01 | unrun-verify | src/AkariToolbox.App/Services/TweakHandlers/DefenderTweakHandler.cs |  | Task 2 human-check verify (real Windows machine: Tamper Protection ON/OFF flow, SHA256 integrity gate rejection, RunOnce cleanup write) not run — no live Windows test machine available in this automated worktree execution; pinned SHA256 hashes computed via direct HTTPS download+hash rather than Get-FileHash on a locally-downloaded C:\\PostInstall copy | open |  | 2026-09-01T00:27:11.046Z |  |
| 2 | 01 | unrun-verify | src/AkariToolbox.App/Services/TweakCatalog.cs |  | Task 2 elevated-launch human-check (32 tweaks render, 3 spot-checked registry values, Home/nav render) not run — requires live elevated Windows session; deferred to end-of-phase UAT per workflow.human_verify_mode=end-of-phase | open |  | 2026-09-01T00:41:35.852Z |  |
| 3 | 02 | unrun-verify | src/AkariToolbox.App/Services/TweakHandlers/GamingGraphicsTweaks.cs |  | Elevated manual UI/registry smoke test for Hdcp/P0State/MsiMode toggles (Task 1 tracer human-check) deferred to end-of-phase UAT per human_verify_mode=end-of-phase | open |  | 2026-09-01T10:58:02.419Z |  |
| 4 | 02 | unrun-verify | src/AkariToolbox.App/Views/GamingTweaksPage.xaml |  | Elevated manual launch: Gaming Tweaks page shows 5 toggles (Hdcp,P0State,MsiMode,AmdSettings,IntelSettings) plus 2 working D-05 shortcut buttons — deferred to end-of-phase UAT | open |  | 2026-09-01T11:14:22.994Z |  |

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
  },
  {
    "id": 2,
    "kind": "unrun-verify",
    "phase": "01",
    "file": "src/AkariToolbox.App/Services/TweakCatalog.cs",
    "line": null,
    "description": "Task 2 elevated-launch human-check (32 tweaks render, 3 spot-checked registry values, Home/nav render) not run — requires live elevated Windows session; deferred to end-of-phase UAT per workflow.human_verify_mode=end-of-phase",
    "status": "open",
    "reason": "",
    "recorded_at": "2026-09-01T00:41:35.852Z",
    "resolved_at": null
  },
  {
    "id": 3,
    "kind": "unrun-verify",
    "phase": "02",
    "file": "src/AkariToolbox.App/Services/TweakHandlers/GamingGraphicsTweaks.cs",
    "line": null,
    "description": "Elevated manual UI/registry smoke test for Hdcp/P0State/MsiMode toggles (Task 1 tracer human-check) deferred to end-of-phase UAT per human_verify_mode=end-of-phase",
    "status": "open",
    "reason": "",
    "recorded_at": "2026-09-01T10:58:02.419Z",
    "resolved_at": null
  },
  {
    "id": 4,
    "kind": "unrun-verify",
    "phase": "02",
    "file": "src/AkariToolbox.App/Views/GamingTweaksPage.xaml",
    "line": null,
    "description": "Elevated manual launch: Gaming Tweaks page shows 5 toggles (Hdcp,P0State,MsiMode,AmdSettings,IntelSettings) plus 2 working D-05 shortcut buttons — deferred to end-of-phase UAT",
    "status": "open",
    "reason": "",
    "recorded_at": "2026-09-01T11:14:22.994Z",
    "resolved_at": null
  }
]
````
