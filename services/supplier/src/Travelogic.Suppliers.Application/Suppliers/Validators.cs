using FluentValidation;
using Travelogic.Suppliers.Domain.Suppliers;

namespace Travelogic.Suppliers.Application.Suppliers;

// Validators give the caller every problem with a request at once, with field paths the UI can bind
// to. The domain still guards its own invariants; this is the friendly first line.

public sealed class AddressValidator : AbstractValidator<AddressDto>
{
    public AddressValidator()
    {
        RuleFor(x => x.Line1).MaximumLength(200);
        RuleFor(x => x.Line2).MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Region).MaximumLength(100);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PostalCode).MaximumLength(20);
    }
}

public sealed class ContactValidator : AbstractValidator<ContactDto>
{
    public ContactValidator()
    {
        RuleFor(x => x.Email).MaximumLength(256).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone)
            .MaximumLength(30)
            .Matches(@"^\+?[0-9 ()\-]{7,30}$").WithMessage("Phone may only contain digits, spaces, brackets, dashes and a leading +.")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));
        RuleFor(x => x.Website)
            .MaximumLength(256)
            .Must(BeAbsoluteHttpUrl).WithMessage("Website must be a full http or https address.")
            .When(x => !string.IsNullOrWhiteSpace(x.Website));
    }

    private static bool BeAbsoluteHttpUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}

public sealed class ServiceRequestValidator : AbstractValidator<ServiceRequest>
{
    public ServiceRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(SupplierService.NameMaxLength);
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Description).MaximumLength(SupplierService.DescriptionMaxLength);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, ignoreTrailingZeros: true);
        RuleFor(x => x.Currency)
            .NotEmpty()
            .Matches("^[A-Za-z]{3}$").WithMessage("Currency must be a three letter ISO 4217 code, e.g. ZAR.");
        RuleFor(x => x.PricingUnit).IsInEnum();
        RuleFor(x => x.DurationMinutes).GreaterThan(0).LessThanOrEqualTo(60 * 24 * 30).When(x => x.DurationMinutes.HasValue);
        RuleFor(x => x.Capacity).GreaterThan(0).LessThanOrEqualTo(10_000).When(x => x.Capacity.HasValue);
    }
}

public sealed class CreateSupplierRequestValidator : AbstractValidator<CreateSupplierRequest>
{
    public CreateSupplierRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Supplier.NameMaxLength);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Description).MaximumLength(Supplier.DescriptionMaxLength);
        RuleFor(x => x.Address).NotNull().SetValidator(new AddressValidator());
        RuleFor(x => x.Contact!).SetValidator(new ContactValidator()).When(x => x.Contact is not null);

        RuleFor(x => x.Services)
            .Must(s => s!.Count <= Supplier.MaxServices).WithMessage($"A supplier cannot have more than {Supplier.MaxServices} services.")
            .Must(HaveUniqueNames).WithMessage("Each service must have a different name.")
            .When(x => x.Services is not null);
        RuleForEach(x => x.Services).NotNull().SetValidator(new ServiceRequestValidator());
    }

    private static bool HaveUniqueNames(IReadOnlyList<ServiceRequest>? services) =>
        services is null || services
            .Where(s => s is not null && !string.IsNullOrWhiteSpace(s.Name))
            .GroupBy(s => s.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .All(g => g.Count() == 1);
}

public sealed class UpdateSupplierRequestValidator : AbstractValidator<UpdateSupplierRequest>
{
    public UpdateSupplierRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Supplier.NameMaxLength);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Description).MaximumLength(Supplier.DescriptionMaxLength);
        RuleFor(x => x.Address).NotNull().SetValidator(new AddressValidator());
        RuleFor(x => x.Contact!).SetValidator(new ContactValidator()).When(x => x.Contact is not null);
        RuleFor(x => x.Version).NotEmpty().WithMessage("Version is required. Use the value returned when the supplier was read.");
    }
}
