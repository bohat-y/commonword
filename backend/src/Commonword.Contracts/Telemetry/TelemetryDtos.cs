using System.Text.Json;

namespace Commonword.Contracts.Telemetry;

public sealed record TelemetryEventRequest(
    string Client,
    string PlayerId,
    string Type,
    JsonElement Payload,
    Guid? SessionId,
    DateTimeOffset? OccurredAt);
