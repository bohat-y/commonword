using Commonword.Contracts.Puzzles;

namespace Commonword.Contracts.Solving;

public sealed record StartSessionRequest(Guid PuzzleId, string PlayerId);

public sealed record UpsertEntryRequest(string? Value);

public sealed record EntryDto(int Row, int Col, string Value, DateTimeOffset UpdatedAt);

public sealed record CheckWordRequest(string Direction, int Number);

public sealed record CellCoordinateDto(int Row, int Col);

public sealed record CheckWordResponse(
    bool Complete,
    bool Correct,
    IReadOnlyList<CellCoordinateDto>? IncorrectCells);

public sealed record SolveSessionDto(
    Guid Id,
    Guid PuzzleId,
    string PlayerId,
    DateTimeOffset StartedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? CompletedAt);

public sealed record SolveSessionDetailsDto(
    SolveSessionDto Session,
    PuzzlePublicDto Puzzle,
    IReadOnlyList<EntryDto> Entries);
