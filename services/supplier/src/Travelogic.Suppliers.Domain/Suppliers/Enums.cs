namespace Travelogic.Suppliers.Domain.Suppliers;

// Stored and serialised as strings.

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

public enum ServiceCategory
{
    Accommodation = 1,
    Activity = 2,
    Tour = 3,
    Transfer = 4,
    Meal = 5,
    Other = 99,
}

public enum PricingUnit
{
    PerPerson = 1,
    PerNight = 2,
    PerRoomPerNight = 3,
    PerGroup = 4,
    PerVehicle = 5,
    PerBooking = 6,
}
