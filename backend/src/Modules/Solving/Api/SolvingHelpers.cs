using Commonword.Contracts.Puzzles;
using Commonword.Contracts.Solving;

namespace Commonword.Modules.Solving.Api;

internal static class SolvingHelpers
{
    internal static Dictionary<string, string[]> ValidateStartSession(StartSessionRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (request.PuzzleId == Guid.Empty)
            errors["puzzleId"] = ["PuzzleId is required."];

        if (string.IsNullOrWhiteSpace(request.PlayerId))
            errors["playerId"] = ["PlayerId is required."];

        return errors;
    }

    internal static Dictionary<string, string[]> ValidateEntry(int row, int col, UpsertEntryRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (row < 0 || col < 0)
            errors["cell"] = ["Row and column must be zero or greater."];

        if (!string.IsNullOrWhiteSpace(request.Value) && request.Value.Trim().Length != 1)
            errors["value"] = ["Value must be a single character."];

        return errors;
    }

    internal static Dictionary<string, string[]> ValidateCheckWord(CheckWordRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(request.Direction)
            || (!request.Direction.Equals("across", StringComparison.OrdinalIgnoreCase)
                && !request.Direction.Equals("down", StringComparison.OrdinalIgnoreCase)))
        {
            errors["direction"] = ["Direction must be 'across' or 'down'."];
        }

        if (request.Number <= 0)
            errors["number"] = ["Number must be a positive integer."];

        return errors;
    }

    internal static string NormalizeDirection(string direction)
        => direction.Equals("down", StringComparison.OrdinalIgnoreCase) ? "down" : "across";

    internal static char GetSolutionChar(PuzzleSolutionDto solution, int row, int col)
    {
        if (row < 0 || row >= solution.SolutionGrid.Count)
            return '\0';

        var rowValue = solution.SolutionGrid[row];
        if (col < 0 || col >= rowValue.Length)
            return '\0';

        return char.ToUpperInvariant(rowValue[col]);
    }
}
