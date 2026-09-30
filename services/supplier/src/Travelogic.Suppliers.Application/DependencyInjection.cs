using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Travelogic.Suppliers.Application.Suppliers;

namespace Travelogic.Suppliers.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateSupplierRequestValidator>(ServiceLifetime.Singleton);
        services.AddScoped<SupplierCommands>();
        return services;
    }
}
