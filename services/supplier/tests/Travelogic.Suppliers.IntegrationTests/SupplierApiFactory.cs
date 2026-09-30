using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace Travelogic.Suppliers.IntegrationTests;

/// <summary>
/// Hosts the real API in memory against a real SQL Server in a throwaway container, so the tests
/// cover EF Core mappings, migrations, constraints and rowversion behaviour, not a fake.
/// </summary>
public sealed class SupplierApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly MsSqlContainer _sqlServer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public async Task InitializeAsync() => await _sqlServer.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _sqlServer.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connectionString = new SqlConnectionStringBuilder(_sqlServer.GetConnectionString())
        {
            InitialCatalog = "TravelogicSuppliersTests",
        }.ConnectionString;

        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:SuppliersDb", connectionString);
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.UseSetting("Database:SeedSampleData", "false");
        builder.UseSetting("Outbox:PollingInterval", "00:00:00.200");
    }
}

[CollectionDefinition(Name)]
public sealed class ApiTestGroup : ICollectionFixture<SupplierApiFactory>
{
    public const string Name = "Supplier API";
}
