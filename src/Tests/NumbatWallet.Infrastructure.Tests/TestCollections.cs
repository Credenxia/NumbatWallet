using Xunit;

namespace NumbatWallet.Infrastructure.Tests;

[CollectionDefinition("Database Collection", DisableParallelization = true)]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public class DatabaseTestCollection : ICollectionFixture<object>
#pragma warning restore CA1711
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}