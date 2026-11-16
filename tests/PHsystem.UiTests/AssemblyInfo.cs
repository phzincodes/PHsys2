using Xunit;

// WPF's package/BAML cache is process-wide. Independent STA tests must not load it concurrently.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
