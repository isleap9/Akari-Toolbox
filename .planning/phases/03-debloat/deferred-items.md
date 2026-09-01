# Deferred Items — Phase 3 (Debloat)

## 03-01

- **Pre-existing test failure, out of scope**: `AkariToolbox.Tests.ConvertersTests.EnumToBoolean_matches_parameter` fails in this build environment — it asserts `converter.ConvertBack(...)` throws `System.Runtime.InteropServices.COMException` when reaching `DependencyProperty.UnsetValue` in a headless unit-test host, but no exception is thrown here. This is unrelated to Debloat (touches `ConvertersTests.cs`/`EnumToBooleanConverter`, neither modified by 03-01) and is environment-dependent (WinRT activation-context behavior), not a regression introduced by this plan. Not fixed per the deviation rules' scope boundary (pre-existing failure in an unrelated file).
