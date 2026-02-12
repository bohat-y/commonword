using System.Text.Json;

namespace Commonword.Modules.Puzzles.Domain;

public sealed class Puzzle
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset ImportedAt { get; set; }
    public bool IsDaily { get; set; }
    public JsonElement Data { get; set; }
    public JsonElement? DataPrivate { get; set; }
}
