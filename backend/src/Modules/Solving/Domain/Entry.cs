namespace Commonword.Modules.Solving.Domain;

public sealed class Entry
{
    public Guid SessionId { get; set; }
    public int Row { get; set; }
    public int Col { get; set; }
    public string Value { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }

    public SolveSession? Session { get; set; }
}
