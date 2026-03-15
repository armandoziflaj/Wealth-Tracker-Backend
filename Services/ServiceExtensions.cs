using System.Reflection;

namespace WealthTracker.Services;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var serviceTypes = assembly.GetTypes()
            .Where(t => t.Name.EndsWith("Service") && t.IsClass && !t.IsAbstract);

        foreach (var serviceType in serviceTypes)
        {
            var interfaceType = serviceType.GetInterface($"I{serviceType.Name}");

            if (interfaceType != null)
            {
                services.AddScoped(interfaceType, serviceType);
            }
        }
        return services;
    }
}