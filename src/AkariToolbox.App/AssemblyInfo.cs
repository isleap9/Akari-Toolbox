using System.Runtime.CompilerServices;

// Grants AkariToolbox.Tests access to internal members (e.g.
// AkariOSTweaksViewModel.TryGetStateAsync) that are exposed for test seams only,
// per Plan 01-07 Task 1.
[assembly: InternalsVisibleTo("AkariToolbox.Tests")]
