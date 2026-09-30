using Travelogic.Suppliers.Domain.Common;
using Travelogic.Suppliers.Domain.Suppliers;

namespace Travelogic.Suppliers.UnitTests.Domain;

public class ValueObjectTests
{
    [Fact]
    public void Money_normalises_currency_and_compares_by_value()
    {
        Money.Of(100m, " usd ").ShouldBe(Money.Of(100.00m, "USD"));
    }

    [Theory]
    [InlineData(-1, "ZAR")]
    [InlineData(10.555, "ZAR")]
    [InlineData(10, "RAND")]
    [InlineData(10, "Z1R")]
    [InlineData(10, "")]
    public void Money_rejects_invalid_values(decimal amount, string currency)
    {
        Should.Throw<DomainException>(() => Money.Of(amount, currency));
    }

    [Fact]
    public void Address_trims_and_turns_blank_optionals_into_null()
    {
        var address = Address.Create("  ", null, " Cape Town ", "", "South Africa", null);

        address.Line1.ShouldBeNull();
        address.Region.ShouldBeNull();
        address.City.ShouldBe("Cape Town");
    }

    [Fact]
    public void Address_requires_city_and_country()
    {
        Should.Throw<DomainException>(() => Address.Create(null, null, "", null, "South Africa", null));
        Should.Throw<DomainException>(() => Address.Create(null, null, "Cape Town", null, " ", null));
    }
}
