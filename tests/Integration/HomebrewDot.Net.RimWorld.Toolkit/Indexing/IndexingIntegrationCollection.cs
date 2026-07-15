using Xunit;

namespace HomebrewDot.Net.RimWorld.Tests.Indexing
{
    /// <summary>
    /// Serializes indexing integration tests to prevent shared static state interference
    /// between tests that register schema handlers and indexers.
    /// </summary>
    [CollectionDefinition("IndexingIntegration", DisableParallelization = true)]
    public class IndexingIntegrationCollection { }
}
