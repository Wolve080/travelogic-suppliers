namespace Travelogic.Suppliers.Domain.Common;

internal static class Guard
{
    public static string Required(string? value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{field} is required.");
        }

        return MaxLength(value.Trim(), field, maxLength);
    }

    public static string? Optional(string? value, string field, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? null : MaxLength(value.Trim(), field, maxLength);

    public static int? PositiveOrNull(int? value, string field) =>
        value is null or > 0 ? value : throw new DomainException($"{field} must be greater than zero.");

    public static TEnum Defined<TEnum>(TEnum value, string field) where TEnum : struct, Enum =>
        Enum.IsDefined(value) ? value : throw new DomainException($"{field} \"{value}\" is not a recognised value.");

    private static string MaxLength(string value, string field, int maxLength) =>
        value.Length > maxLength
            ? throw new DomainException($"{field} must be {maxLength} characters or fewer.")
            : value;
}
