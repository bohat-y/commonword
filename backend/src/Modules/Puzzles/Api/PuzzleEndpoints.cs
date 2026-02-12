using System.Text.Json;
using Commonword.Contracts.Puzzles;
using Commonword.Infrastructure.Auth;
using Commonword.Infrastructure.Persistence;
using Commonword.Infrastructure.Time;
using Commonword.Modules.Puzzles.Application;
using Commonword.Modules.Puzzles.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Commonword.Modules.Puzzles.Api;

internal static class PuzzleEndpoints
{
    public static IEndpointRouteBuilder MapPuzzlesEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/puzzles").WithTags("Puzzles");
        var adminGroup = endpoints.MapGroup("/admin/puzzles").WithTags("Admin");
        adminGroup.AddEndpointFilter<AdminKeyEndpointFilter>();

        group.MapGet("/{id:guid}", GetPuzzleById);
        group.MapGet("/today", GetToday);

        adminGroup.MapPost("/import/ipuz", ImportIpuz)
            .Accepts<JsonElement>("application/json");
        adminGroup.MapPost("/{id:guid}/mark-daily", MarkDaily);

        return endpoints;
    }

    private static async Task<IResult> ImportIpuz(
        [FromHeader(Name = "X-Admin-Key")] string? adminKey,
        JsonElement ipuz,
        AppDbContext db,
        IClock clock,
        CancellationToken cancellationToken)
    {
        if (ipuz.ValueKind == JsonValueKind.Undefined || ipuz.ValueKind == JsonValueKind.Null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["body"] = new[] { "Request body is required." }
            });
        }

        if (!IpuzImporter.TryParse(ipuz, out var importResult, out var errors))
        {
            return Results.ValidationProblem(errors);
        }

        var puzzle = new Puzzle
        {
            Id = Guid.NewGuid(),
            Title = importResult.Title,
            ImportedAt = clock.UtcNow,
            IsDaily = false,
            Data = JsonSerializer.SerializeToElement(importResult.PublicData, JsonDefaults.Options),
            DataPrivate = importResult.Solution is null
                ? null
                : JsonSerializer.SerializeToElement(importResult.Solution, JsonDefaults.Options)
        };

        db.Set<Puzzle>().Add(puzzle);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/puzzles/{puzzle.Id}", ToDto(puzzle));
    }

    private static async Task<IResult> GetPuzzleById(
        Guid id,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var puzzle = await db.Set<Puzzle>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (puzzle is null)
        {
            return Results.Problem(title: "Puzzle not found.", statusCode: StatusCodes.Status404NotFound);
        }

        if (!TryToDto(puzzle, out var dto, out var error))
        {
            return error;
        }

        return Results.Ok(dto);
    }

    private static async Task<IResult> MarkDaily(
        Guid id,
        [FromHeader(Name = "X-Admin-Key")] string? adminKey,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var puzzle = await db.Set<Puzzle>()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (puzzle is null)
        {
            return Results.Problem(title: "Puzzle not found.", statusCode: StatusCodes.Status404NotFound);
        }

        puzzle.IsDaily = true;

        await db.Set<Puzzle>()
            .Where(p => p.IsDaily && p.Id != id)
            .ExecuteUpdateAsync(update => update.SetProperty(p => p.IsDaily, false), cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> GetToday(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var puzzle = await db.Set<Puzzle>()
            .AsNoTracking()
            .Where(p => p.IsDaily)
            .OrderByDescending(p => p.ImportedAt)
            .FirstOrDefaultAsync(cancellationToken);

        puzzle ??= await db.Set<Puzzle>()
            .AsNoTracking()
            .OrderByDescending(p => p.ImportedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (puzzle is null)
        {
            return Results.Problem(title: "No puzzles imported yet.", statusCode: StatusCodes.Status404NotFound);
        }

        if (!TryToDto(puzzle, out var dto, out var error))
        {
            return error;
        }

        return Results.Ok(dto);
    }

    private static PuzzlePublicDto ToDto(Puzzle puzzle)
    {
        var data = puzzle.Data.Deserialize<PuzzlePublicDataDto>(JsonDefaults.Options)
            ?? throw new InvalidOperationException("Stored puzzle data was invalid.");

        return new PuzzlePublicDto(
            puzzle.Id,
            puzzle.Title,
            puzzle.IsDaily,
            puzzle.ImportedAt,
            data);
    }

    private static bool TryToDto(Puzzle puzzle, out PuzzlePublicDto dto, out IResult error)
    {
        try
        {
            dto = ToDto(puzzle);
            error = Results.Ok();
            return true;
        }
        catch (JsonException)
        {
            dto = default!;
            error = Results.Problem(title: "Stored puzzle data is invalid.", statusCode: StatusCodes.Status500InternalServerError);
            return false;
        }
    }

}
