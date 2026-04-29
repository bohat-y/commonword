using Commonword.Contracts.Puzzles;
using Commonword.Contracts.Solving;
using Commonword.Modules.Solving.Api;
using FluentAssertions;
using Xunit;

namespace Commonword.Tests.Unit.Solving;

public class SolvingHelpersTests
{
    // ValidateStartSession

    [Fact]
    public void ValidateStartSession_ValidRequest_ReturnsNoErrors()
    {
        var request = new StartSessionRequest(Guid.NewGuid(), "player-1");
        SolvingHelpers.ValidateStartSession(request).Should().BeEmpty();
    }

    [Fact]
    public void ValidateStartSession_EmptyPuzzleId_ReturnsError()
    {
        var request = new StartSessionRequest(Guid.Empty, "player-1");
        var errors = SolvingHelpers.ValidateStartSession(request);
        errors.Should().ContainKey("puzzleId");
    }

    [Fact]
    public void ValidateStartSession_NullPlayerId_ReturnsError()
    {
        var request = new StartSessionRequest(Guid.NewGuid(), null!);
        var errors = SolvingHelpers.ValidateStartSession(request);
        errors.Should().ContainKey("playerId");
    }

    [Fact]
    public void ValidateStartSession_WhitespacePlayerId_ReturnsError()
    {
        var request = new StartSessionRequest(Guid.NewGuid(), "   ");
        var errors = SolvingHelpers.ValidateStartSession(request);
        errors.Should().ContainKey("playerId");
    }

    [Fact]
    public void ValidateStartSession_BothInvalid_ReturnsBothErrors()
    {
        var request = new StartSessionRequest(Guid.Empty, "");
        var errors = SolvingHelpers.ValidateStartSession(request);
        errors.Should().ContainKey("puzzleId").And.ContainKey("playerId");
    }

    // ValidateEntry

    [Fact]
    public void ValidateEntry_ValidRowColAndSingleChar_ReturnsNoErrors()
    {
        var request = new UpsertEntryRequest("A");
        SolvingHelpers.ValidateEntry(0, 0, request).Should().BeEmpty();
    }

    [Fact]
    public void ValidateEntry_NullValue_ReturnsNoErrors()
    {
        var request = new UpsertEntryRequest(null);
        SolvingHelpers.ValidateEntry(2, 3, request).Should().BeEmpty();
    }

    [Fact]
    public void ValidateEntry_EmptyValue_ReturnsNoErrors()
    {
        var request = new UpsertEntryRequest("");
        SolvingHelpers.ValidateEntry(0, 0, request).Should().BeEmpty();
    }

    [Fact]
    public void ValidateEntry_NegativeRow_ReturnsCellError()
    {
        var request = new UpsertEntryRequest("A");
        var errors = SolvingHelpers.ValidateEntry(-1, 0, request);
        errors.Should().ContainKey("cell");
    }

    [Fact]
    public void ValidateEntry_NegativeCol_ReturnsCellError()
    {
        var request = new UpsertEntryRequest("A");
        var errors = SolvingHelpers.ValidateEntry(0, -1, request);
        errors.Should().ContainKey("cell");
    }

    [Fact]
    public void ValidateEntry_BothNegative_ReturnsCellError()
    {
        var request = new UpsertEntryRequest("A");
        var errors = SolvingHelpers.ValidateEntry(-1, -5, request);
        errors.Should().ContainKey("cell");
    }

    [Fact]
    public void ValidateEntry_MultiCharValue_ReturnsValueError()
    {
        var request = new UpsertEntryRequest("AB");
        var errors = SolvingHelpers.ValidateEntry(0, 0, request);
        errors.Should().ContainKey("value");
    }

    [Fact]
    public void ValidateEntry_WhitespaceOnlyValue_ReturnsNoErrors()
    {
        var request = new UpsertEntryRequest("   ");
        SolvingHelpers.ValidateEntry(0, 0, request).Should().BeEmpty();
    }

    [Fact]
    public void ValidateEntry_NegativeRowAndMultiChar_ReturnsBothErrors()
    {
        var request = new UpsertEntryRequest("AB");
        var errors = SolvingHelpers.ValidateEntry(-1, 0, request);
        errors.Should().ContainKey("cell").And.ContainKey("value");
    }

    // ValidateCheckWord

    [Fact]
    public void ValidateCheckWord_ValidAcross_ReturnsNoErrors()
    {
        var request = new CheckWordRequest("across", 1);
        SolvingHelpers.ValidateCheckWord(request).Should().BeEmpty();
    }

    [Fact]
    public void ValidateCheckWord_ValidDown_ReturnsNoErrors()
    {
        var request = new CheckWordRequest("down", 3);
        SolvingHelpers.ValidateCheckWord(request).Should().BeEmpty();
    }

    [Fact]
    public void ValidateCheckWord_DirectionCaseInsensitive_ReturnsNoErrors()
    {
        var request = new CheckWordRequest("ACROSS", 1);
        SolvingHelpers.ValidateCheckWord(request).Should().BeEmpty();
    }

    [Fact]
    public void ValidateCheckWord_InvalidDirection_ReturnsDirectionError()
    {
        var request = new CheckWordRequest("diagonal", 1);
        var errors = SolvingHelpers.ValidateCheckWord(request);
        errors.Should().ContainKey("direction");
    }

    [Fact]
    public void ValidateCheckWord_EmptyDirection_ReturnsDirectionError()
    {
        var request = new CheckWordRequest("", 1);
        var errors = SolvingHelpers.ValidateCheckWord(request);
        errors.Should().ContainKey("direction");
    }

    [Fact]
    public void ValidateCheckWord_ZeroNumber_ReturnsNumberError()
    {
        var request = new CheckWordRequest("across", 0);
        var errors = SolvingHelpers.ValidateCheckWord(request);
        errors.Should().ContainKey("number");
    }

    [Fact]
    public void ValidateCheckWord_NegativeNumber_ReturnsNumberError()
    {
        var request = new CheckWordRequest("down", -1);
        var errors = SolvingHelpers.ValidateCheckWord(request);
        errors.Should().ContainKey("number");
    }

    [Fact]
    public void ValidateCheckWord_BothInvalid_ReturnsBothErrors()
    {
        var request = new CheckWordRequest("sideways", 0);
        var errors = SolvingHelpers.ValidateCheckWord(request);
        errors.Should().ContainKey("direction").And.ContainKey("number");
    }

    // NormalizeDirection

    [Theory]
    [InlineData("down")]
    [InlineData("DOWN")]
    [InlineData("Down")]
    public void NormalizeDirection_DownVariants_ReturnsDown(string input)
    {
        SolvingHelpers.NormalizeDirection(input).Should().Be("down");
    }

    [Theory]
    [InlineData("across")]
    [InlineData("ACROSS")]
    [InlineData("Across")]
    [InlineData("diagonal")]
    [InlineData("anything")]
    public void NormalizeDirection_NonDown_ReturnsAcross(string input)
    {
        SolvingHelpers.NormalizeDirection(input).Should().Be("across");
    }

    // GetSolutionChar

    private static PuzzleSolutionDto Grid(params string[] rows)
        => new(rows);

    [Fact]
    public void GetSolutionChar_ValidPosition_ReturnsUppercaseChar()
    {
        var solution = Grid("cat", "dog");
        SolvingHelpers.GetSolutionChar(solution, 0, 0).Should().Be('C');
    }

    [Fact]
    public void GetSolutionChar_LowercaseInGrid_ReturnsUppercase()
    {
        var solution = Grid("cat");
        SolvingHelpers.GetSolutionChar(solution, 0, 2).Should().Be('T');
    }

    [Fact]
    public void GetSolutionChar_LastCell_ReturnsCorrectChar()
    {
        var solution = Grid("abc", "xyz");
        SolvingHelpers.GetSolutionChar(solution, 1, 2).Should().Be('Z');
    }

    [Fact]
    public void GetSolutionChar_NegativeRow_ReturnsNul()
    {
        var solution = Grid("cat");
        SolvingHelpers.GetSolutionChar(solution, -1, 0).Should().Be('\0');
    }

    [Fact]
    public void GetSolutionChar_RowBeyondGrid_ReturnsNul()
    {
        var solution = Grid("cat");
        SolvingHelpers.GetSolutionChar(solution, 5, 0).Should().Be('\0');
    }

    [Fact]
    public void GetSolutionChar_NegativeCol_ReturnsNul()
    {
        var solution = Grid("cat");
        SolvingHelpers.GetSolutionChar(solution, 0, -1).Should().Be('\0');
    }

    [Fact]
    public void GetSolutionChar_ColBeyondRow_ReturnsNul()
    {
        var solution = Grid("cat");
        SolvingHelpers.GetSolutionChar(solution, 0, 10).Should().Be('\0');
    }

    [Fact]
    public void GetSolutionChar_EmptyGrid_ReturnsNul()
    {
        var solution = Grid();
        SolvingHelpers.GetSolutionChar(solution, 0, 0).Should().Be('\0');
    }
}
