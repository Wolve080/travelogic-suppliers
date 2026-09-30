using FluentValidation;
using FluentValidation.Results;

namespace Travelogic.Suppliers.Application.Common;

internal static class ValidationExtensions
{
    public static async Task<Error?> ValidateToErrorAsync<T>(this IValidator<T> validator, T instance, CancellationToken cancellationToken)
    {
        if (instance is null)
        {
            return Error.Validation("A request body is required.");
        }

        var result = await validator.ValidateAsync(instance, cancellationToken);
        return result.IsValid ? null : ToError(result);
    }

    private static Error ToError(ValidationResult result)
    {
        var details = result.Errors
            .GroupBy(e => ToJsonPath(e.PropertyName))
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());

        return Error.Validation("One or more fields are invalid.", details);
    }

    // "Services[0].PricingUnit" -> "services[0].pricingUnit", so keys match the JSON the client sent.
    private static string ToJsonPath(string propertyName) =>
        string.Join('.', propertyName.Split('.').Select(segment =>
            segment.Length == 0 ? segment : char.ToLowerInvariant(segment[0]) + segment[1..]));
}
