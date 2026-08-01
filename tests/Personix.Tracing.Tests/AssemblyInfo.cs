using Xunit;

// ActivityHelper keeps a static ActivitySource, so tests must not run concurrently.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
