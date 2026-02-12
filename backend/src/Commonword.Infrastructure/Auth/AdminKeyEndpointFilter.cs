using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Commonword.Infrastructure.Auth;

public sealed class AdminKeyEndpointFilter : IEndpointFilter
{
    private readonly IConfiguration _configuration;

    public AdminKeyEndpointFilter(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var expected = _configuration["Admin:Key"] ?? _configuration["Admin__Key"];
        if (string.IsNullOrWhiteSpace(expected))
        {
            return Results.Unauthorized();
        }

        if (!context.HttpContext.Request.Headers.TryGetValue("X-Admin-Key", out var provided)
            || string.IsNullOrWhiteSpace(provided))
        {
            return Results.Unauthorized();
        }

        if (!string.Equals(provided.ToString(), expected, StringComparison.Ordinal))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        return await next(context);
    }
}
