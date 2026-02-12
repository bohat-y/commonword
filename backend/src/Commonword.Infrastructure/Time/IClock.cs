namespace Commonword.Infrastructure.Time;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
