using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Travelogic.Suppliers.Domain.Suppliers;

namespace Travelogic.Suppliers.Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    // dev/demo only, prod should run a migration bundle in the pipeline
    public bool MigrateOnStartup { get; init; }

    public bool SeedSampleData { get; init; }
}

public static partial class DatabaseInitializer
{
    public static async Task InitialiseDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var options = services.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        if (!options.MigrateOnStartup && !options.SeedSampleData)
        {
            return;
        }

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SuppliersDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseInitializer));

        if (options.MigrateOnStartup)
        {
            LogMigrating(logger);
            await db.Database.MigrateAsync(cancellationToken);
        }

        if (options.SeedSampleData && !await db.Suppliers.AnyAsync(cancellationToken))
        {
            var suppliers = SampleData.Suppliers();
            db.Suppliers.AddRange(suppliers);
            await db.SaveChangesAsync(cancellationToken);
            LogSeeded(logger, suppliers.Count);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Applying database migrations")]
    private static partial void LogMigrating(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded {Count} sample suppliers")]
    private static partial void LogSeeded(ILogger logger, int count);
}

internal static class SampleData
{
    public static List<Supplier> Suppliers()
    {
        var hotel = Supplier.Create(
            "Table Bay View Hotel",
            SupplierType.Accommodation,
            "Four star hotel on the V&A Waterfront with views of Table Mountain.",
            ContactDetails.Create("reservations@tablebayview.example", "+27 21 555 0100", "https://tablebayview.example"),
            Address.Create("12 Dock Road", null, "Cape Town", "Western Cape", "South Africa", "8001"));
        hotel.AddService("Deluxe Sea View Room", ServiceCategory.Accommodation, "King bed, balcony, breakfast included.", Money.Of(3450m, "ZAR"), PricingUnit.PerRoomPerNight, null, 2);
        hotel.AddService("Family Suite", ServiceCategory.Accommodation, "Two bedrooms, sleeps four.", Money.Of(5900m, "ZAR"), PricingUnit.PerRoomPerNight, null, 4);
        hotel.AddService("Harbour Dinner", ServiceCategory.Meal, "Three course seafood dinner.", Money.Of(650m, "ZAR"), PricingUnit.PerPerson, 120, null);

        var safari = Supplier.Create(
            "Kruger Horizons Safaris",
            SupplierType.Safari,
            "Guided game drives in and around the Greater Kruger.",
            ContactDetails.Create("bookings@krugerhorizons.example", "+27 13 555 0199", null),
            Address.Create(null, null, "Hazyview", "Mpumalanga", "South Africa", "1242"));
        safari.AddService("Half Day Game Drive", ServiceCategory.Activity, "Morning drive in an open vehicle with a ranger.", Money.Of(1450m, "ZAR"), PricingUnit.PerPerson, 240, 9);
        safari.AddService("Full Day Kruger Safari", ServiceCategory.Tour, "Includes park fees and a packed lunch.", Money.Of(2950m, "ZAR"), PricingUnit.PerPerson, 600, 9);
        safari.AddService("Sunset Bush Walk", ServiceCategory.Activity, null, Money.Of(890m, "ZAR"), PricingUnit.PerPerson, 180, 8);

        var transfers = Supplier.Create(
            "Winelands Shuttle Co.",
            SupplierType.Transport,
            "Airport transfers and private drivers across the Cape Winelands.",
            ContactDetails.Create("hello@winelandsshuttle.example", "+27 21 555 0142", "https://winelandsshuttle.example"),
            Address.Create("4 Plein Street", null, "Stellenbosch", "Western Cape", "South Africa", "7600"));
        transfers.AddService("Cape Town Airport Transfer", ServiceCategory.Transfer, "One way, up to 3 passengers.", Money.Of(850m, "ZAR"), PricingUnit.PerVehicle, 60, 3);

        return [hotel, safari, transfers];
    }
}
