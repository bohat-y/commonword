using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;

namespace Commonword.Modules.Telemetry.Api;

public static class TelemetryModule
{
    public static IServiceCollection AddTelemetryModule(this IServiceCollection services)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapTelemetryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        return TelemetryEndpoints.MapTelemetryEndpoints(endpoints);
    }

    public static Assembly Assembly => typeof(TelemetryModule).Assembly;
}
