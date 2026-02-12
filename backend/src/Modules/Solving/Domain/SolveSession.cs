namespace Commonword.Modules.Solving.Domain;

public sealed class SolveSession
{
    public Guid Id { get; set; }
    public Guid PuzzleId { get; set; }
    public string PlayerId { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public ICollection<Entry> Entries { get; set; } = new List<Entry>();
}
