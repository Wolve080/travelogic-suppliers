namespace Travelogic.Suppliers.Domain.Suppliers;

// Enums are persisted and serialised as strings, so adding a value is a non-breaking change
// for the database and for API consumers.

/// <summary>The kind of business a supplier is.</summary>
public enum SupplierType
{
    Accommodation = 1,
    TourOperator = 2,
    Safari = 3,
    Transport = 4,
    Restaurant = 5,
    ActivityProvider = 6,
    Other = 99,
}

/// <summary>What a supplier's service is, e.g. an overnight stay or a half day game drive.</summary>
public enum ServiceCategory
{
    Accommodation = 1,
    Activity = 2,
    Tour = 3,
    Transfer = 4,
    Meal = 5,
    Other = 99,
}

/// <summary>What the price of a service is quoted against.</summary>
public enum PricingUnit
{
    PerPerson = 1,
    PerNight = 2,
    PerRoomPerNight = 3,
    PerGroup = 4,
    PerVehicle = 5,
    PerBooking = 6,
}
