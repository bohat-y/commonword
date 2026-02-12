using System.Data;
using System.Text.Json;
using Commonword.Contracts.Puzzles;
using Commonword.Contracts.Solving;
using Commonword.Infrastructure.Persistence;
using Commonword.Infrastructure.Time;
using Commonword.Modules.Solving.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Commonword.Modules.Solving.Api;

internal static class SolvingEndpoints
{
    public static IEndpointRouteBuilder MapSolvingEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/sessions").WithTags("Solving");

        group.MapPost("", StartSession);
        group.MapGet("/{id:guid}", GetSession);
        group.MapPut("/{id:guid}/cells/{row:int}/{col:int}", UpsertEntry);
        group.MapPost("/{id:guid}/check-word", CheckWord);

        return endpoints;
    }

    private static async Task<IResult> StartSession(
        StartSessionRequest request,
        AppDbContext db,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateStartSession(request);
        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        if (!await PuzzleExistsAsync(db, request.PuzzleId, cancellationToken))
        {
            return Results.Problem(title: "Puzzle not found.", statusCode: StatusCodes.Status404NotFound);
        }

        var now = clock.UtcNow;
        var session = new SolveSession
        {
            Id = Guid.NewGuid(),
            PuzzleId = request.PuzzleId,
            PlayerId = request.PlayerId.Trim(),
            StartedAt = now,
            UpdatedAt = now
        };

        db.Set<SolveSession>().Add(session);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/sessions/{session.Id}", ToDto(session));
    }

    private static async Task<IResult> UpsertEntry(
        Guid id,
        int row,
        int col,
        UpsertEntryRequest request,
        AppDbContext db,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateEntry(row, col, request);
        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        var session = await db.Set<SolveSession>()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (session is null)
        {
            return Results.Problem(title: "Session not found.", statusCode: StatusCodes.Status404NotFound);
        }

        var entry = await db.Set<Entry>().FindAsync(new object?[] { id, row, col }, cancellationToken);
        var now = clock.UtcNow;

        if (string.IsNullOrWhiteSpace(request.Value))
        {
            if (entry is not null)
            {
                db.Set<Entry>().Remove(entry);
            }

            session.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(new EntryDto(row, col, string.Empty, now));
        }

        var normalizedValue = request.Value.Trim().Substring(0, 1).ToUpperInvariant();

        if (entry is null)
        {
            entry = new Entry
            {
                SessionId = id,
                Row = row,
                Col = col,
                Value = normalizedValue,
                UpdatedAt = now
            };

            db.Set<Entry>().Add(entry);
        }
        else
        {
            entry.Value = normalizedValue;
            entry.UpdatedAt = now;
        }

        session.UpdatedAt = now;

        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(new EntryDto(entry.Row, entry.Col, entry.Value, entry.UpdatedAt));
    }

    private static async Task<IResult> GetSession(
        Guid id,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var session = await db.Set<SolveSession>()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (session is null)
        {
            return Results.Problem(title: "Session not found.", statusCode: StatusCodes.Status404NotFound);
        }

        var entries = await db.Set<Entry>()
            .AsNoTracking()
            .Where(e => e.SessionId == id)
            .ToListAsync(cancellationToken);

        PuzzleLoadResult? puzzleResult;
        try
        {
            puzzleResult = await LoadPuzzleAsync(db, session.PuzzleId, cancellationToken);
        }
        catch (JsonException)
        {
            return Results.Problem(title: "Stored puzzle data is invalid.", statusCode: StatusCodes.Status500InternalServerError);
        }

        if (puzzleResult is null)
        {
            return Results.Problem(title: "Puzzle not found.", statusCode: StatusCodes.Status404NotFound);
        }

        var sessionDto = ToDto(session);
        var entryDtos = entries
            .Select(entry => new EntryDto(entry.Row, entry.Col, entry.Value, entry.UpdatedAt))
            .ToList();

        return Results.Ok(new SolveSessionDetailsDto(sessionDto, puzzleResult.Puzzle, entryDtos));
    }

    private static async Task<IResult> CheckWord(
        Guid id,
        CheckWordRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateCheckWord(request);
        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        var session = await db.Set<SolveSession>()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (session is null)
        {
            return Results.Problem(title: "Session not found.", statusCode: StatusCodes.Status404NotFound);
        }

        PuzzleLoadResult? puzzleResult;
        try
        {
            puzzleResult = await LoadPuzzleAsync(db, session.PuzzleId, cancellationToken);
        }
        catch (JsonException)
        {
            return Results.Problem(title: "Stored puzzle data is invalid.", statusCode: StatusCodes.Status500InternalServerError);
        }

        if (puzzleResult is null)
        {
            return Results.Problem(title: "Puzzle not found.", statusCode: StatusCodes.Status404NotFound);
        }

        if (puzzleResult.PublicData.WordIndex is null)
        {
            return Results.Problem(title: "Word index is not available for this puzzle.", statusCode: StatusCodes.Status409Conflict);
        }

        if (puzzleResult.Solution is null)
        {
            return Results.Problem(title: "Puzzle solution is not available.", statusCode: StatusCodes.Status409Conflict);
        }

        var direction = NormalizeDirection(request.Direction);
        var wordIndex = direction == "across"
            ? puzzleResult.PublicData.WordIndex.Across
            : puzzleResult.PublicData.WordIndex.Down;

        if (!wordIndex.TryGetValue(request.Number, out var wordEntry))
        {
            return Results.Problem(title: "Word not found.", statusCode: StatusCodes.Status404NotFound);
        }

        var entries = await db.Set<Entry>()
            .AsNoTracking()
            .Where(e => e.SessionId == id)
            .ToListAsync(cancellationToken);

        var entryLookup = entries.ToDictionary(entry => (entry.Row, entry.Col));
        var incorrectCells = new List<CellCoordinateDto>();
        var complete = true;

        for (var i = 0; i < wordEntry.Length; i++)
        {
            var row = wordEntry.Row + (direction == "down" ? i : 0);
            var col = wordEntry.Col + (direction == "across" ? i : 0);

            if (!entryLookup.TryGetValue((row, col), out var entry)
                || string.IsNullOrWhiteSpace(entry.Value))
            {
                complete = false;
                continue;
            }

            var expected = GetSolutionChar(puzzleResult.Solution, row, col);
            var actual = char.ToUpperInvariant(entry.Value.Trim()[0]);

            if (expected == '\0')
            {
                return Results.Problem(title: "Stored puzzle solution is invalid.", statusCode: StatusCodes.Status500InternalServerError);
            }

            if (actual != expected)
            {
                incorrectCells.Add(new CellCoordinateDto(row, col));
            }
        }

        var correct = complete && incorrectCells.Count == 0;
        var response = new CheckWordResponse(
            complete,
            correct,
            incorrectCells.Count == 0 ? null : incorrectCells);

        return Results.Ok(response);
    }

    private static SolveSessionDto ToDto(SolveSession session)
        => new(
            session.Id,
            session.PuzzleId,
            session.PlayerId,
            session.StartedAt,
            session.UpdatedAt,
            session.CompletedAt);

    private static Dictionary<string, string[]> ValidateStartSession(StartSessionRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (request.PuzzleId == Guid.Empty)
        {
            errors["puzzleId"] = new[] { "PuzzleId is required." };
        }

        if (string.IsNullOrWhiteSpace(request.PlayerId))
        {
            errors["playerId"] = new[] { "PlayerId is required." };
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateEntry(int row, int col, UpsertEntryRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (row < 0 || col < 0)
        {
            errors["cell"] = new[] { "Row and column must be zero or greater." };
        }

        if (!string.IsNullOrWhiteSpace(request.Value) && request.Value.Trim().Length != 1)
        {
            errors["value"] = new[] { "Value must be a single character." };
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateCheckWord(CheckWordRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(request.Direction)
            || (!request.Direction.Equals("across", StringComparison.OrdinalIgnoreCase)
                && !request.Direction.Equals("down", StringComparison.OrdinalIgnoreCase)))
        {
            errors["direction"] = new[] { "Direction must be 'across' or 'down'." };
        }

        if (request.Number <= 0)
        {
            errors["number"] = new[] { "Number must be a positive integer." };
        }

        return errors;
    }

    private static async Task<bool> PuzzleExistsAsync(
        AppDbContext db,
        Guid puzzleId,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State == ConnectionState.Closed;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "select 1 from puzzles where id = @id";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "id";
        parameter.Value = puzzleId;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync(cancellationToken);

        if (shouldClose)
        {
            await connection.CloseAsync();
        }

        return result is not null;
    }

    private static async Task<PuzzleLoadResult?> LoadPuzzleAsync(
        AppDbContext db,
        Guid puzzleId,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State == ConnectionState.Closed;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "select id, title, imported_at, is_daily, data, data_private from puzzles where id = @id";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "id";
        parameter.Value = puzzleId;
        command.Parameters.Add(parameter);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }

            return null;
        }

        var id = reader.GetGuid(0);
        var title = reader.GetString(1);
        var importedAt = reader.GetFieldValue<DateTimeOffset>(2);
        var isDaily = reader.GetBoolean(3);
        var rawData = reader.GetValue(4);
        var dataJson = rawData switch
        {
            string json => json,
            JsonDocument doc => doc.RootElement.GetRawText(),
            JsonElement element => element.GetRawText(),
            _ => rawData?.ToString() ?? string.Empty
        };

        var privateJson = string.Empty;
        if (!reader.IsDBNull(5))
        {
            var rawPrivate = reader.GetValue(5);
            privateJson = rawPrivate switch
            {
                string json => json,
                JsonDocument doc => doc.RootElement.GetRawText(),
                JsonElement element => element.GetRawText(),
                _ => rawPrivate?.ToString() ?? string.Empty
            };
        }

        if (string.IsNullOrWhiteSpace(dataJson))
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }

            return null;
        }

        if (shouldClose)
        {
            await connection.CloseAsync();
        }

        var data = JsonSerializer.Deserialize<PuzzlePublicDataDto>(dataJson, JsonDefaults.Options);
        if (data is null)
        {
            return null;
        }

        PuzzleSolutionDto? solution = null;
        if (!string.IsNullOrWhiteSpace(privateJson))
        {
            solution = JsonSerializer.Deserialize<PuzzleSolutionDto>(privateJson, JsonDefaults.Options);
        }

        var puzzle = new PuzzlePublicDto(id, title, isDaily, importedAt, data);
        return new PuzzleLoadResult(puzzle, data, solution);
    }

    private static string NormalizeDirection(string direction)
        => direction.Equals("down", StringComparison.OrdinalIgnoreCase) ? "down" : "across";

    private static char GetSolutionChar(PuzzleSolutionDto solution, int row, int col)
    {
        if (row < 0 || row >= solution.SolutionGrid.Count)
        {
            return '\0';
        }

        var rowValue = solution.SolutionGrid[row];
        if (col < 0 || col >= rowValue.Length)
        {
            return '\0';
        }

        return char.ToUpperInvariant(rowValue[col]);
    }

    private sealed record PuzzleLoadResult(
        PuzzlePublicDto Puzzle,
        PuzzlePublicDataDto PublicData,
        PuzzleSolutionDto? Solution);
}
