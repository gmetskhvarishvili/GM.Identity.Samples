using Xunit;

// Every test class here is an integration test against shared Postgres + Redis, and the reconciliation
// jobs do global cache prunes. Run the assembly serially so tests never race over that shared state.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
