# Phase 02 Deferred Items

Out-of-scope discoveries logged during plan execution per the executor's Scope
Boundary rule (fix only what the current task's changes directly caused).

## From Plan 02-01

- **`AkariToolbox.Tests.ConvertersTests.EnumToBoolean_matches_parameter` fails
  in the full `dotnet test` run.** Pre-existing — last touched in Phase 1
  commit `61efb3f`, not modified by any 02-01 commit. Expects a
  `COMException` from `DispatcherQueue.GetForCurrentThread()` off a real
  WinRT-activated UI thread; whether it throws appears environment-dependent
  (observed passing during Phase 1, failing in this worktree's headless test
  run). Unrelated to `TweakCategory`/Gaming Tweaks changes — not fixed here
  per Scope Boundary. Worth a look before the next full-phase test run if it
  recurs.
