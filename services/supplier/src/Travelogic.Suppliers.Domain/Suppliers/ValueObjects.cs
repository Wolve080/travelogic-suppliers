using System.Globalization;
using Travelogic.Suppliers.Domain.Common;

namespace Travelogic.Suppliers.Domain.Suppliers;

public sealed record Money
{
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public static Money Of(decimal amount, string? currency)
    {
        if (amount < 0)
        {
            throw new DomainException("Price cannot be negative.");
        }

        if (decimal.Round(amount, 2) != amount)
        {
            throw new DomainException("Price cannot have more than two decimal places.");
        }

        var code = currency?.Trim().ToUpperInvariant();
        if (code is not { Length: 3 } || !code.All(char.IsAsciiLetterUpper))
        {
            throw new DomainException("Currency must be a three letter ISO 4217 code, e.g. ZAR.");
        }

        return new Money(amount, code);
    }

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Currency} {Amount:0.00}");
}

public sealed record Address
{
    private Address(string? line1, string? line2, string city, string? region, string country, string? postalCode)
    {
        Line1 = line1;
        Line2 = line2;
        City = city;
        Region = region;
        Country = country;
        PostalCode = postalCode;
    }

    public string? Line1 { get; }

    public string? Line2 { get; }

    public string City { get; }

    public string? Region { get; }

    public string Country { get; }

    public string? PostalCode { get; }

    public static Address Create(string? line1, string? line2, string? city, string? region, string? country, string? postalCode) =>
        new(
            Guard.Optional(line1, "Address line 1", 200),
            Guard.Optional(line2, "Address line 2", 200),
            Guard.Required(city, "City", 100),
            Guard.Optional(region, "Region", 100),
            Guard.Required(country, "Country", 100),
            Guard.Optional(postalCode, "Postal code", 20));
}

// Email/URL format is checked by the validators.
public sealed record ContactDetails
{
    private ContactDetails(string? email, string? phone, string? website)
    {
        Email = email;
        Phone = phone;
        Website = website;
    }

    public string? Email { get; }

    public string? Phone { get; }

    public string? Website { get; }

    public static ContactDetails Create(string? email, string? phone, string? website) =>
        new(
            Guard.Optional(email, "Email", 256),
            Guard.Optional(phone, "Phone", 30),
            Guard.Optional(website, "Website", 256));
}
