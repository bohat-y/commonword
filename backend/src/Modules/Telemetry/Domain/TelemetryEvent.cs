using System.Text.Json;

namespace Commonword.Modules.Telemetry.Domain;

public sealed class TelemetryEvent
{
    public Guid Id { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string Client { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public JsonElement Payload { get; set; }
    public Guid? SessionId { get; set; }
}
