using Commonword.Contracts.Telemetry;
using Commonword.Infrastructure.Persistence;
using Commonword.Infrastructure.Time;
using Commonword.Modules.Telemetry.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Commonword.Modules.Telemetry.Api;

internal static class TelemetryEndpoints
{
    public static IEndpointRouteBuilder MapTelemetryEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/telemetry").WithTags("Telemetry");

        group.MapPost("/events", async (
            TelemetryEventRequest request,
            AppDbContext db,
            IClock clock,
            CancellationToken cancellationToken) =>
        {
            var validationErrors = Validate(request);
            if (validationErrors.Count > 0)
            {
                return Results.ValidationProblem(validationErrors);
            }

            var telemetryEvent = new TelemetryEvent
            {
                Id = Guid.NewGuid(),
                OccurredAt = request.OccurredAt ?? clock.UtcNow,
                Client = request.Client.Trim(),
                PlayerId = request.PlayerId.Trim(),
                Type = request.Type.Trim(),
                Payload = request.Payload,
                SessionId = request.SessionId
            };

            db.Set<TelemetryEvent>().Add(telemetryEvent);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Created($"/telemetry/events/{telemetryEvent.Id}", new { telemetryEvent.Id });
        });

        return endpoints;
    }

    private static Dictionary<string, string[]> Validate(TelemetryEventRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(request.Client))
        {
            errors["client"] = new[] { "Client is required." };
        }

        if (string.IsNullOrWhiteSpace(request.PlayerId))
        {
            errors["playerId"] = new[] { "PlayerId is required." };
        }

        if (string.IsNullOrWhiteSpace(request.Type))
        {
            errors["type"] = new[] { "Type is required." };
        }

        return errors;
    }
}
