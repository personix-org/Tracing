using Microsoft.Extensions.DependencyInjection;

namespace Personix.Tracing;

/// <summary>Registers <see cref="ActivityHelper"/>'s activity source during dependency-injection start-up.</summary>
public static class TracingServiceRegistration
{
    /// <summary>Configures <see cref="ActivityHelper.Source"/> for the application, so it is set before the OpenTelemetry pipeline starts listening.</summary>
    /// <param name="services">The collection to return unchanged, so calls can be chained.</param>
    /// <param name="activitySourceName">
    /// The name to start activities under. Must match the name the OpenTelemetry pipeline listens to,
    /// or nothing will be exported. Defaults to <c>"Application"</c>.
    /// </param>
    /// <returns><paramref name="services"/>, for chaining further registration calls.</returns>
    /// <remarks>
    /// This does not register any services in <paramref name="services"/> — it only has the side effect
    /// of calling <see cref="ActivityHelper.Configure"/>. Call it once, during start-up, not per request.
    /// </remarks>
    public static IServiceCollection AddApplicationTracing(this IServiceCollection services, string activitySourceName = "Application")
    {
        ActivityHelper.Configure(activitySourceName);
        return services;
    }
}
