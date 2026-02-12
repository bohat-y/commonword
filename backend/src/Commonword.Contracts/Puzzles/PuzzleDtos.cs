using System.Text.Json;
using System.Text.Json.Serialization;

namespace Commonword.Contracts.Puzzles;

public sealed record PuzzleDataDto(
    int Width,
    int Height,
    IReadOnlyList<string> Grid,
    PuzzleCluesDto Clues,
    PuzzleMetadataDto? Metadata);

public sealed record PuzzlePublicDto(
    Guid Id,
    string Title,
    bool IsDaily,
    DateTimeOffset ImportedAt,
    PuzzlePublicDataDto Data);

public sealed record PuzzlePublicDataDto(
    int Version,
    int Width,
    int Height,
    IReadOnlyList<PuzzleBlockCellDto> BlockCells,
    PuzzleCluesDto Clues,
    PuzzleWordIndexDto? WordIndex,
    PuzzleMetaDto? Meta);

public sealed record PuzzleBlockCellDto(int Row, int Col);

public sealed record PuzzleWordIndexDto(
    IReadOnlyDictionary<int, PuzzleWordIndexEntryDto> Across,
    IReadOnlyDictionary<int, PuzzleWordIndexEntryDto> Down);

public sealed record PuzzleWordIndexEntryDto(int Row, int Col, int Length);

public sealed record PuzzleMetaDto(
    string? Author,
    string? Title,
    string? Source);

public sealed record PuzzleSolutionDto(IReadOnlyList<string> SolutionGrid);

public sealed record PuzzleClueDto(int Number, string Text);

public sealed record PuzzleCluesDto(
    IReadOnlyList<PuzzleClueDto> Across,
    IReadOnlyList<PuzzleClueDto> Down);

public sealed record PuzzleMetadataDto(
    string? Author,
    string? Source,
    string? Notes);

public sealed record ImportPuzzleRequest(
    string Title,
    PuzzleDataDto PuzzleData);

public sealed record PuzzleDto(
    Guid Id,
    string Title,
    bool IsDaily,
    DateTimeOffset ImportedAt,
    PuzzleDataDto PuzzleData);

public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
