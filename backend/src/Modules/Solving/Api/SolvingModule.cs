using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Commonword.Modules.Solving.Api;

public static class SolvingModule
{
    public static IServiceCollection AddSolvingModule(this IServiceCollection services)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapSolvingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        return SolvingEndpoints.MapSolvingEndpoints(endpoints);
    }

    public static Assembly Assembly => typeof(SolvingModule).Assembly;
}
