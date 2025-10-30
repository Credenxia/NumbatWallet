namespace NumbatWallet.Web.Api.Tests.TestHelpers;

/// <summary>
/// Collection definition for tests that must run sequentially to avoid test isolation issues
/// </summary>
[CollectionDefinition("Sequential", DisableParallelization = true)]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix - xUnit requires 'Collection' suffix
public class SequentialTestCollection
#pragma warning restore CA1711
{
}