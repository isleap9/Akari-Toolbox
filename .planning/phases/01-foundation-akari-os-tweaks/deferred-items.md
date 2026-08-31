# Deferred Items — Phase 1

Items discovered during execution that are out of scope for the current plan
(pre-existing, unrelated to the task's own changes) per the executor's scope
boundary rule.

## From Plan 01-01

- **`AkariToolbox.Tests.ConvertersTests.EnumToBoolean_matches_parameter` — flaky/environment-dependent, pre-existing.**
  Copied verbatim from the `WinUI-3-MVVM-Framework` template's own
  `AppTemplate.Tests/ConvertersTests.cs` (unmodified by this plan). The test
  asserts `Assert.Throws<COMException>(...)` when `EnumToBooleanConverter.ConvertBack`
  reaches `DependencyProperty.UnsetValue` — the template's own comment notes
  this WinRT static "cannot be resolved in the unit-test host" and is expected
  to throw `COMException` there. In this execution environment the call did
  not throw (test host WinRT/COM initialization state differs). Not caused by
  any change in this plan — out of scope to fix here. Revisit if xUnit/WinAppSDK
  test-host tooling changes, or drop the assertion if it proves environment-fragile
  across CI runners.
