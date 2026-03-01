using Xunit;

namespace SADC.Order.Management.Tests.Integration;

/// <summary>
/// xUnit collection definition that shares a single TestWebApplicationFactory
/// across all integration test classes. This prevents the Serilog "logger is
/// already frozen" error that occurs when multiple WebApplicationFactory instances
/// each invoke Program.Main and freeze the static ReloadableLogger.
/// </summary>
[CollectionDefinition("Integration")]
public class IntegrationTestCollection : ICollectionFixture<TestWebApplicationFactory>
{
}
