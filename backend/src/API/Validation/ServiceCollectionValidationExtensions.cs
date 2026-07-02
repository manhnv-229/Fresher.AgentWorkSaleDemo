using System.Reflection;

using FluentValidation;

namespace Demo.Api.Validation;

/// <summary>
/// Hỗ trợ đăng ký validator theo assembly mà không phụ thuộc integration package riêng của FluentValidation.
/// </summary>
public static class ServiceCollectionValidationExtensions
{
    public static IServiceCollection AddValidatorsFromAssemblies(this IServiceCollection services, params Assembly[] assemblies)
    {
        var registrations = assemblies
            .Distinct()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .SelectMany(type => type.GetInterfaces()
                .Where(@interface =>
                    @interface.IsGenericType &&
                    @interface.GetGenericTypeDefinition() == typeof(IValidator<>))
                .Select(@interface => new { ServiceType = @interface, ImplementationType = type }))
            .ToList();

        foreach (var registration in registrations)
        {
            services.AddScoped(registration.ServiceType, registration.ImplementationType);
        }

        return services;
    }
}
