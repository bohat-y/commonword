using System.Text.Json;
using Commonword.Contracts.Puzzles;

namespace Commonword.Modules.Puzzles.Application;

internal sealed record IpuzImportResult(
    string Title,
    PuzzlePublicDataDto PublicData,
    PuzzleSolutionDto? Solution);

internal static class IpuzImporter
{
    public static bool TryParse(
        JsonElement root,
        out IpuzImportResult result,
        out Dictionary<string, string[]> errors)
    {
        errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        result = default!;

        if (!TryGetDimensions(root, out var width, out var height, errors))
        {
            return false;
        }

        var blockMarker = GetString(root, "block") ?? "#";

        if (!TryGetPuzzleGrid(root, width, height, blockMarker, out var isBlock, out var blockCells, errors))
        {
            return false;
        }

        var wordIndex = ComputeWordIndex(width, height, isBlock);
        var clues = ParseClues(root);
        var meta = BuildMeta(root);

        PuzzleSolutionDto? solution = null;
        if (!TryGetSolution(root, width, height, blockMarker, isBlock, out solution, errors))
        {
            return false;
        }

        var title = GetString(root, "title")?.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            title = meta?.Title?.Trim();
        }

        title = string.IsNullOrWhiteSpace(title) ? "Untitled" : title;

        var publicData = new PuzzlePublicDataDto(
            Version: 1,
            Width: width,
            Height: height,
            BlockCells: blockCells,
            Clues: clues,
            WordIndex: wordIndex,
            Meta: meta);

        result = new IpuzImportResult(title, publicData, solution);
        return errors.Count == 0;
    }

    private static bool TryGetDimensions(
        JsonElement root,
        out int width,
        out int height,
        Dictionary<string, string[]> errors)
    {
        width = 0;
        height = 0;

        if (!TryGetProperty(root, "dimensions", out var dimensions)
            || dimensions.ValueKind != JsonValueKind.Object)
        {
            errors["dimensions"] = new[] { "Dimensions are required." };
            return false;
        }

        if (!TryGetInt(dimensions, "width", out width) || width <= 0)
        {
            errors["dimensions.width"] = new[] { "Width must be a positive integer." };
        }

        if (!TryGetInt(dimensions, "height", out height) || height <= 0)
        {
            errors["dimensions.height"] = new[] { "Height must be a positive integer." };
        }

        return errors.Count == 0;
    }

    private static bool TryGetPuzzleGrid(
        JsonElement root,
        int width,
        int height,
        string blockMarker,
        out bool[,] isBlock,
        out List<PuzzleBlockCellDto> blockCells,
        Dictionary<string, string[]> errors)
    {
        isBlock = new bool[height, width];
        blockCells = new List<PuzzleBlockCellDto>();

        if (!TryGetProperty(root, "puzzle", out var puzzle)
            || puzzle.ValueKind != JsonValueKind.Array)
        {
            errors["puzzle"] = new[] { "Puzzle grid is required." };
            return false;
        }

        if (puzzle.GetArrayLength() != height)
        {
            errors["puzzle"] = new[] { "Puzzle grid height does not match dimensions." };
            return false;
        }

        for (var row = 0; row < height; row++)
        {
            var rowElement = puzzle[row];
            if (rowElement.ValueKind != JsonValueKind.Array || rowElement.GetArrayLength() != width)
            {
                errors[$"puzzle[{row}]"] = new[] { "Puzzle grid row does not match width." };
                return false;
            }

            for (var col = 0; col < width; col++)
            {
                var cell = rowElement[col];
                var block = cell.ValueKind == JsonValueKind.String
                    && string.Equals(cell.GetString(), blockMarker, StringComparison.Ordinal);

                if (block)
                {
                    isBlock[row, col] = true;
                    blockCells.Add(new PuzzleBlockCellDto(row, col));
                }
            }
        }

        return true;
    }

    private static PuzzleWordIndexDto ComputeWordIndex(int width, int height, bool[,] isBlock)
    {
        var across = new Dictionary<int, PuzzleWordIndexEntryDto>();
        var down = new Dictionary<int, PuzzleWordIndexEntryDto>();
        var nextNumber = 1;

        for (var row = 0; row < height; row++)
        {
            for (var col = 0; col < width; col++)
            {
                if (isBlock[row, col])
                {
                    continue;
                }

                var acrossLength = GetAcrossLength(width, isBlock, row, col);
                var downLength = GetDownLength(height, isBlock, row, col);

                if (acrossLength == 0 && downLength == 0)
                {
                    continue;
                }

                var number = nextNumber++;

                if (acrossLength > 0)
                {
                    across[number] = new PuzzleWordIndexEntryDto(row, col, acrossLength);
                }

                if (downLength > 0)
                {
                    down[number] = new PuzzleWordIndexEntryDto(row, col, downLength);
                }
            }
        }

        return new PuzzleWordIndexDto(across, down);
    }

    private static int GetAcrossLength(int width, bool[,] isBlock, int row, int col)
    {
        if (col > 0 && !isBlock[row, col - 1])
        {
            return 0;
        }

        if (col + 1 >= width || isBlock[row, col + 1])
        {
            return 0;
        }

        var length = 0;
        for (var c = col; c < width && !isBlock[row, c]; c++)
        {
            length++;
        }

        return length;
    }

    private static int GetDownLength(int height, bool[,] isBlock, int row, int col)
    {
        if (row > 0 && !isBlock[row - 1, col])
        {
            return 0;
        }

        if (row + 1 >= height || isBlock[row + 1, col])
        {
            return 0;
        }

        var length = 0;
        for (var r = row; r < height && !isBlock[r, col]; r++)
        {
            length++;
        }

        return length;
    }

    private static PuzzleCluesDto ParseClues(JsonElement root)
    {
        if (!TryGetProperty(root, "clues", out var cluesElement)
            || cluesElement.ValueKind != JsonValueKind.Object)
        {
            return new PuzzleCluesDto(Array.Empty<PuzzleClueDto>(), Array.Empty<PuzzleClueDto>());
        }

        var across = ParseClueList(cluesElement, "across");
        var down = ParseClueList(cluesElement, "down");

        return new PuzzleCluesDto(across, down);
    }

    private static IReadOnlyList<PuzzleClueDto> ParseClueList(JsonElement cluesElement, string direction)
    {
        if (!TryGetProperty(cluesElement, direction, out var listElement)
            || listElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<PuzzleClueDto>();
        }

        var clues = new List<PuzzleClueDto>();
        foreach (var item in listElement.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Object)
            {
                if (!TryGetProperty(item, "number", out var numberElement)
                    || !TryGetNumber(numberElement, out var number))
                {
                    continue;
                }

                var clueText = GetString(item, "clue") ?? string.Empty;
                clues.Add(new PuzzleClueDto(number, clueText));
                continue;
            }

            if (item.ValueKind == JsonValueKind.Array)
            {
                var arrayLength = item.GetArrayLength();
                if (arrayLength < 2)
                {
                    continue;
                }

                if (!TryGetNumber(item[0], out var number))
                {
                    continue;
                }

                var clueText = item[1].ValueKind == JsonValueKind.String
                    ? item[1].GetString() ?? string.Empty
                    : item[1].ToString();

                clues.Add(new PuzzleClueDto(number, clueText));
            }
        }

        return clues;
    }

    private static PuzzleMetaDto? BuildMeta(JsonElement root)
    {
        var author = GetString(root, "author");
        var title = GetString(root, "title");
        var source = GetString(root, "source") ?? GetString(root, "publisher");

        if (string.IsNullOrWhiteSpace(author)
            && string.IsNullOrWhiteSpace(title)
            && string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        return new PuzzleMetaDto(
            string.IsNullOrWhiteSpace(author) ? null : author,
            string.IsNullOrWhiteSpace(title) ? null : title,
            string.IsNullOrWhiteSpace(source) ? null : source);
    }

    private static bool TryGetSolution(
        JsonElement root,
        int width,
        int height,
        string blockMarker,
        bool[,] isBlock,
        out PuzzleSolutionDto? solution,
        Dictionary<string, string[]> errors)
    {
        solution = null;

        if (!TryGetProperty(root, "solution", out var solutionElement))
        {
            return true;
        }

        if (solutionElement.ValueKind != JsonValueKind.Array)
        {
            errors["solution"] = new[] { "Solution must be a 2D array if provided." };
            return false;
        }

        if (solutionElement.GetArrayLength() != height)
        {
            errors["solution"] = new[] { "Solution height does not match dimensions." };
            return false;
        }

        var rows = new List<string>(height);
        for (var row = 0; row < height; row++)
        {
            var rowElement = solutionElement[row];
            if (rowElement.ValueKind != JsonValueKind.Array || rowElement.GetArrayLength() != width)
            {
                errors[$"solution[{row}]"] = new[] { "Solution row does not match width." };
                return false;
            }

            var chars = new char[width];
            for (var col = 0; col < width; col++)
            {
                if (isBlock[row, col])
                {
                    chars[col] = '#';
                    continue;
                }

                var cell = rowElement[col];
                if (cell.ValueKind == JsonValueKind.String)
                {
                    var value = cell.GetString();
                    chars[col] = string.IsNullOrWhiteSpace(value) ? '?' : char.ToUpperInvariant(value[0]);
                }
                else if (cell.ValueKind == JsonValueKind.Number)
                {
                    chars[col] = cell.GetRawText()[0];
                }
                else
                {
                    chars[col] = '?';
                }
            }

            rows.Add(new string(chars));
        }

        solution = new PuzzleSolutionDto(rows);
        return true;
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static string? GetString(JsonElement element, string name)
    {
        if (!TryGetProperty(element, name, out var value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
    }

    private static bool TryGetInt(JsonElement element, string name, out int value)
    {
        value = 0;
        if (!TryGetProperty(element, name, out var valueElement))
        {
            return false;
        }

        return TryGetNumber(valueElement, out value);
    }

    private static bool TryGetNumber(JsonElement element, out int value)
    {
        value = 0;
        if (element.ValueKind == JsonValueKind.Number)
        {
            return element.TryGetInt32(out value);
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            return int.TryParse(element.GetString(), out value);
        }

        return false;
    }
}
