using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Travelogic.Suppliers.Application.Abstractions;
using Travelogic.Suppliers.Application.Suppliers;
using Travelogic.Suppliers.Infrastructure.Outbox;
using Travelogic.Suppliers.Infrastructure.Persistence;

namespace Travelogic.Suppliers.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "SuppliersDb";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

        services.TryAddSingleton(TimeProvider.System);

        services.AddDbContext<SuppliersDbContext>(options => options.UseSqlServer(connectionString, sql =>
        {
            sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
            sql.MigrationsHistoryTable("__EFMigrationsHistory", SuppliersDbContext.Schema);
        }));

        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ISupplierQueries, SupplierQueries>();

        services.AddOptions<DatabaseOptions>().Bind(configuration.GetSection(DatabaseOptions.SectionName));

        services.AddOptions<OutboxOptions>()
            .Bind(configuration.GetSection(OutboxOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.TryAddSingleton<IIntegrationEventPublisher, LoggingIntegrationEventPublisher>();

        if (configuration.GetSection(OutboxOptions.SectionName).GetValue(nameof(OutboxOptions.Enabled), defaultValue: true))
        {
            services.AddHostedService<OutboxProcessor>();
        }

        services.AddHealthChecks()
            .AddDbContextCheck<SuppliersDbContext>("database", tags: ["ready"]);

        return services;
    }
}
