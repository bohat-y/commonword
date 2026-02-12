using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Commonword.Modules.Puzzles.Api;

public static class PuzzlesModule
{
    public static IServiceCollection AddPuzzlesModule(this IServiceCollection services)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapPuzzlesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        return PuzzleEndpoints.MapPuzzlesEndpoints(endpoints);
    }

    public static Assembly Assembly => typeof(PuzzlesModule).Assembly;
}
