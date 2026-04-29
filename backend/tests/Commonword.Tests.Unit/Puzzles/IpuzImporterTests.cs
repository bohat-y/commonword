using System.Text.Json;
using Commonword.Modules.Puzzles.Application;
using FluentAssertions;
using Xunit;

namespace Commonword.Tests.Unit.Puzzles;

public class IpuzImporterTests
{
    // Helpers
    private static JsonElement Parse(string json)
        => JsonDocument.Parse(json).RootElement;

    private static JsonElement MinimalValid(int width = 3, int height = 3, string? blockMarker = null)
    {
        var marker = blockMarker ?? "#";
        var row = string.Join(",", Enumerable.Range(0, width).Select(c => $"\"{(c == 0 ? marker : "a")}\""));
        var rows = string.Join(",", Enumerable.Range(0, height).Select(_ => $"[{row}]"));
        var blockProp = blockMarker is not null ? $",\"block\":\"{blockMarker}\"" : string.Empty;
        return Parse($$"""{"dimensions":{"width":{{width}},"height":{{height}}},"puzzle":[{{rows}}]{{blockProp}}}""");
    }

    // Happy path

    [Fact]
    public void TryParse_MinimalValidIpuz_ReturnsTrueWithNoErrors()
    {
        var ipuz = MinimalValid();

        var success = IpuzImporter.TryParse(ipuz, out var result, out var errors);

        success.Should().BeTrue();
        errors.Should().BeEmpty();
        result.Should().NotBeNull();
        result.PublicData.Width.Should().Be(3);
        result.PublicData.Height.Should().Be(3);
    }

    [Fact]
    public void TryParse_MinimalValidIpuz_TitleDefaultsToUntitled()
    {
        var ipuz = MinimalValid();

        IpuzImporter.TryParse(ipuz, out var result, out _);

        result.Title.Should().Be("Untitled");
    }

    [Fact]
    public void TryParse_FullIpuz_PopulatesAllFields()
    {
        var ipuz = Parse("""
            {
              "title": "My Puzzle",
              "author": "Alice",
              "source": "Daily News",
              "dimensions": { "width": 3, "height": 3 },
              "puzzle": [
                ["a", "b", "c"],
                ["#", "d", "e"],
                ["f", "g", "h"]
              ],
              "clues": {
                "across": [{ "number": 1, "clue": "First across" }],
                "down":   [{ "number": 1, "clue": "First down" }]
              },
              "solution": [
                ["A", "B", "C"],
                ["#", "D", "E"],
                ["F", "G", "H"]
              ]
            }
            """);

        var success = IpuzImporter.TryParse(ipuz, out var result, out var errors);

        success.Should().BeTrue();
        errors.Should().BeEmpty();
        result.Title.Should().Be("My Puzzle");
        result.PublicData.Meta.Should().NotBeNull();
        result.PublicData.Meta!.Author.Should().Be("Alice");
        result.PublicData.Meta.Source.Should().Be("Daily News");
        result.PublicData.Clues.Across.Should().HaveCount(1);
        result.PublicData.Clues.Down.Should().HaveCount(1);
        result.Solution.Should().NotBeNull();
    }

    // Dimension validation

    [Fact]
    public void TryParse_MissingDimensions_ReturnsFalseWithDimensionsError()
    {
        var ipuz = Parse("""{"puzzle":[["a"]]}""");

        var success = IpuzImporter.TryParse(ipuz, out _, out var errors);

        success.Should().BeFalse();
        errors.Should().ContainKey("dimensions");
    }

    [Fact]
    public void TryParse_DimensionsIsArray_ReturnsFalseWithDimensionsError()
    {
        var ipuz = Parse("""{"dimensions":[3,3],"puzzle":[["a","b"],["c","d"]]}""");

        var success = IpuzImporter.TryParse(ipuz, out _, out var errors);

        success.Should().BeFalse();
        errors.Should().ContainKey("dimensions");
    }

    [Fact]
    public void TryParse_MissingWidth_ReturnsFalseWithWidthError()
    {
        var ipuz = Parse("""{"dimensions":{"height":2},"puzzle":[["a"],["b"]]}""");

        var success = IpuzImporter.TryParse(ipuz, out _, out var errors);

        success.Should().BeFalse();
        errors.Should().ContainKey("dimensions.width");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void TryParse_NonPositiveWidth_ReturnsFalseWithWidthError(int width)
    {
        var ipuz = Parse($$"""{"dimensions":{"width":{{width}},"height":2},"puzzle":[["a"],["b"]]}""");

        var success = IpuzImporter.TryParse(ipuz, out _, out var errors);

        success.Should().BeFalse();
        errors.Should().ContainKey("dimensions.width");
    }

    [Fact]
    public void TryParse_MissingHeight_ReturnsFalseWithHeightError()
    {
        var ipuz = Parse("""{"dimensions":{"width":2},"puzzle":[["a","b"]]}""");

        var success = IpuzImporter.TryParse(ipuz, out _, out var errors);

        success.Should().BeFalse();
        errors.Should().ContainKey("dimensions.height");
    }

    [Fact]
    public void TryParse_BothDimensionsInvalid_ReturnsBothErrors()
    {
        var ipuz = Parse("""{"dimensions":{"width":0,"height":0},"puzzle":[]}""");

        var success = IpuzImporter.TryParse(ipuz, out _, out var errors);

        success.Should().BeFalse();
        errors.Should().ContainKey("dimensions.width");
        errors.Should().ContainKey("dimensions.height");
    }

    // Puzzle grid validation

    [Fact]
    public void TryParse_MissingPuzzle_ReturnsFalseWithPuzzleError()
    {
        var ipuz = Parse("""{"dimensions":{"width":2,"height":2}}""");

        var success = IpuzImporter.TryParse(ipuz, out _, out var errors);

        success.Should().BeFalse();
        errors.Should().ContainKey("puzzle");
    }

    [Fact]
    public void TryParse_PuzzleIsNotArray_ReturnsFalseWithPuzzleError()
    {
        var ipuz = Parse("""{"dimensions":{"width":2,"height":2},"puzzle":"invalid"}""");

        var success = IpuzImporter.TryParse(ipuz, out _, out var errors);

        success.Should().BeFalse();
        errors.Should().ContainKey("puzzle");
    }

    [Fact]
    public void TryParse_PuzzleRowCountMismatch_ReturnsFalseWithPuzzleError()
    {
        var ipuz = Parse("""{"dimensions":{"width":2,"height":3},"puzzle":[["a","b"],["c","d"]]}""");

        var success = IpuzImporter.TryParse(ipuz, out _, out var errors);

        success.Should().BeFalse();
        errors.Should().ContainKey("puzzle");
    }

    [Fact]
    public void TryParse_PuzzleRowWidthMismatch_ReturnsFalseWithRowError()
    {
        var ipuz = Parse("""{"dimensions":{"width":3,"height":2},"puzzle":[["a","b"],["c","d"]]}""");

        var success = IpuzImporter.TryParse(ipuz, out _, out var errors);

        success.Should().BeFalse();
        errors.Should().ContainKey("puzzle[0]");
    }

    [Fact]
    public void TryParse_PuzzleRowIsNotArray_ReturnsFalseWithRowError()
    {
        var ipuz = Parse("""{"dimensions":{"width":2,"height":2},"puzzle":["notarray",["c","d"]]}""");

        var success = IpuzImporter.TryParse(ipuz, out _, out var errors);

        success.Should().BeFalse();
        errors.Should().ContainKey("puzzle[0]");
    }

    // Block cell detection

    [Fact]
    public void TryParse_DefaultBlockMarker_IdentifiesHashAsBlock()
    {
        var ipuz = Parse("""
            {
              "dimensions": { "width": 3, "height": 1 },
              "puzzle": [["a", "#", "b"]]
            }
            """);

        IpuzImporter.TryParse(ipuz, out var result, out _);

        result.PublicData.BlockCells.Should().ContainSingle(c => c.Row == 0 && c.Col == 1);
        result.PublicData.BlockCells.Should().HaveCount(1);
    }

    [Fact]
    public void TryParse_CustomBlockMarker_IdentifiesCustomMarkerAsBlock()
    {
        var ipuz = Parse("""
            {
              "dimensions": { "width": 3, "height": 1 },
              "block": "*",
              "puzzle": [["a", "*", "b"]]
            }
            """);

        IpuzImporter.TryParse(ipuz, out var result, out _);

        result.PublicData.BlockCells.Should().ContainSingle(c => c.Row == 0 && c.Col == 1);
    }

    [Fact]
    public void TryParse_NoBlockCells_BlockCellsIsEmpty()
    {
        var ipuz = Parse("""
            {
              "dimensions": { "width": 2, "height": 1 },
              "puzzle": [["a", "b"]]
            }
            """);

        IpuzImporter.TryParse(ipuz, out var result, out _);

        result.PublicData.BlockCells.Should().BeEmpty();
    }

    // Word index computation

    [Fact]
    public void TryParse_AcrossWord_AppearsInWordIndex()
    {
        // Row of 3 non-blocks → 1-across word starting at (0,0) length 3
        var ipuz = Parse("""
            {
              "dimensions": { "width": 3, "height": 1 },
              "puzzle": [["a", "b", "c"]]
            }
            """);

        IpuzImporter.TryParse(ipuz, out var result, out _);

        result.PublicData.WordIndex!.Across.Should().ContainKey(1);
        result.PublicData.WordIndex.Across[1].Row.Should().Be(0);
        result.PublicData.WordIndex.Across[1].Col.Should().Be(0);
        result.PublicData.WordIndex.Across[1].Length.Should().Be(3);
    }

    [Fact]
    public void TryParse_DownWord_AppearsInWordIndex()
    {
        // Column of 3 non-blocks → 1-down word starting at (0,0) length 3
        var ipuz = Parse("""
            {
              "dimensions": { "width": 1, "height": 3 },
              "puzzle": [["a"], ["b"], ["c"]]
            }
            """);

        IpuzImporter.TryParse(ipuz, out var result, out _);

        result.PublicData.WordIndex!.Down.Should().ContainKey(1);
        result.PublicData.WordIndex.Down[1].Row.Should().Be(0);
        result.PublicData.WordIndex.Down[1].Col.Should().Be(0);
        result.PublicData.WordIndex.Down[1].Length.Should().Be(3);
    }

    [Fact]
    public void TryParse_CellStartsBothAcrossAndDown_SameNumberAssignedToBoth()
    {
        // 3x3 grid with no blocks → top-left starts both 1-across and 1-down
        var ipuz = Parse("""
            {
              "dimensions": { "width": 3, "height": 3 },
              "puzzle": [
                ["a","b","c"],
                ["d","e","f"],
                ["g","h","i"]
              ]
            }
            """);

        IpuzImporter.TryParse(ipuz, out var result, out _);

        var wordIndex = result.PublicData.WordIndex!;
        wordIndex.Across.Should().ContainKey(1);
        wordIndex.Down.Should().ContainKey(1);
        wordIndex.Across[1].Row.Should().Be(0);
        wordIndex.Down[1].Col.Should().Be(0);
    }

    [Fact]
    public void TryParse_IsolatedCell_NotInWordIndex()
    {
        // Single non-block surrounded by blocks — no across or down word
        var ipuz = Parse("""
            {
              "dimensions": { "width": 3, "height": 3 },
              "puzzle": [
                ["#","#","#"],
                ["#","a","#"],
                ["#","#","#"]
              ]
            }
            """);

        IpuzImporter.TryParse(ipuz, out var result, out _);

        result.PublicData.WordIndex!.Across.Should().BeEmpty();
        result.PublicData.WordIndex.Down.Should().BeEmpty();
    }

    // -------------------------------------------------------------------------
    // Clue parsing
    // -------------------------------------------------------------------------

    [Fact]
    public void TryParse_MissingClues_ReturnsEmptyClueLists()
    {
        var ipuz = MinimalValid();

        IpuzImporter.TryParse(ipuz, out var result, out _);

        result.PublicData.Clues.Across.Should().BeEmpty();
        result.PublicData.Clues.Down.Should().BeEmpty();
    }

    [Fact]
    public void TryParse_ObjectFormatClue_ParsedCorrectly()
    {
        var ipuz = Parse("""
            {
              "dimensions": { "width": 3, "height": 1 },
              "puzzle": [["a","b","c"]],
              "clues": {
                "across": [{ "number": 1, "clue": "Hello world" }],
                "down": []
              }
            }
            """);

        IpuzImporter.TryParse(ipuz, out var result, out _);

        result.PublicData.Clues.Across.Should().ContainSingle(c => c.Number == 1 && c.Text == "Hello world");
    }

    [Fact]
    public void TryParse_ArrayFormatClue_ParsedCorrectly()
    {
        var ipuz = Parse("""
            {
              "dimensions": { "width": 3, "height": 1 },
              "puzzle": [["a","b","c"]],
              "clues": {
                "across": [[1, "Hello world"]],
                "down": []
              }
            }
            """);

        IpuzImporter.TryParse(ipuz, out var result, out _);

        result.PublicData.Clues.Across.Should().ContainSingle(c => c.Number == 1 && c.Text == "Hello world");
    }

    [Fact]
    public void TryParse_MixedClueFormats_BothParsed()
    {
        var ipuz = Parse("""
            {
              "dimensions": { "width": 3, "height": 1 },
              "puzzle": [["a","b","c"]],
              "clues": {
                "across": [
                  { "number": 1, "clue": "Object clue" },
                  [2, "Array clue"]
                ],
                "down": []
              }
            }
            """);

        IpuzImporter.TryParse(ipuz, out var result, out _);

        result.PublicData.Clues.Across.Should().HaveCount(2);
    }

    [Fact]
    public void TryParse_ClueItemMissingNumber_Skipped()
    {
        var ipuz = Parse("""
            {
              "dimensions": { "width": 3, "height": 1 },
              "puzzle": [["a","b","c"]],
              "clues": {
                "across": [{ "clue": "No number here" }],
                "down": []
              }
            }
            """);

        IpuzImporter.TryParse(ipuz, out var result, out _);

        result.PublicData.Clues.Across.Should().BeEmpty();
    }

    [Fact]
    public void TryParse_ArrayClueWithLessThanTwoElements_Skipped()
    {
        var ipuz = Parse("""
            {
              "dimensions": { "width": 3, "height": 1 },
              "puzzle": [["a","b","c"]],
              "clues": {
                "across": [[1]],
                "down": []
              }
            }
            """);

        IpuzImporter.TryParse(ipuz, out var result, out _);

        result.PublicData.Clues.Across.Should().BeEmpty();
    }

    // Meta and title

    [Fact]
    public void TryParse_TitleProperty_UsedAsTitle()
    {
        var ipuz = Parse("""
            {
              "title": "My Crossword",
              "dimensions": { "width": 3, "height": 1 },
              "puzzle": [["a","b","c"]]
            }
            """);

        IpuzImporter.TryParse(ipuz, out var result, out _);

        result.Title.Should().Be("My Crossword");
    }

    [Fact]
    public void TryParse_NoTitleButMetaTitle_FallsBackToMetaTitle()
    {
        var ipuz = Parse("""
            {
              "author": "Someone",
              "dimensions": { "width": 3, "height": 1 },
              "puzzle": [["a","b","c"]]
            }
            """);

        IpuzImporter.TryParse(ipuz, out var result, out _);

        result.Title.Should().Be("Untitled");
    }

    [Fact]
    public void TryParse_NoTitleNoMeta_TitleIsUntitled()
    {
        var ipuz = MinimalValid();

        IpuzImporter.TryParse(ipuz, out var result, out _);

        result.Title.Should().Be("Untitled");
    }

    [Fact]
    public void TryParse_AllMetaFieldsPresent_MetaNotNull()
    {
        var ipuz = Parse("""
            {
              "author": "Alice",
              "source": "Daily",
              "title": "Puzzle",
              "dimensions": { "width": 3, "height": 1 },
              "puzzle": [["a","b","c"]]
            }
            """);

        IpuzImporter.TryParse(ipuz, out var result, out _);

        result.PublicData.Meta.Should().NotBeNull();
        result.PublicData.Meta!.Author.Should().Be("Alice");
        result.PublicData.Meta.Source.Should().Be("Daily");
    }

    [Fact]
    public void TryParse_NoMetaFields_MetaIsNull()
    {
        var ipuz = MinimalValid();

        IpuzImporter.TryParse(ipuz, out var result, out _);

        result.PublicData.Meta.Should().BeNull();
    }

    // Solution parsing

    [Fact]
    public void TryParse_NoSolution_ReturnsTrueWithNullSolution()
    {
        var ipuz = MinimalValid();

        var success = IpuzImporter.TryParse(ipuz, out var result, out var errors);

        success.Should().BeTrue();
        errors.Should().BeEmpty();
        result.Solution.Should().BeNull();
    }

    [Fact]
    public void TryParse_ValidSolution_SolutionRowsBuiltUppercase()
    {
        var ipuz = Parse("""
            {
              "dimensions": { "width": 3, "height": 1 },
              "puzzle": [["a","b","c"]],
              "solution": [["a","b","c"]]
            }
            """);

        IpuzImporter.TryParse(ipuz, out var result, out _);

        result.Solution.Should().NotBeNull();
        result.Solution!.SolutionGrid[0].Should().Be("ABC");
    }

    [Fact]
    public void TryParse_SolutionHeightMismatch_ReturnsFalseWithSolutionError()
    {
        var ipuz = Parse("""
            {
              "dimensions": { "width": 2, "height": 2 },
              "puzzle": [["a","b"],["c","d"]],
              "solution": [["A","B"]]
            }
            """);

        var success = IpuzImporter.TryParse(ipuz, out _, out var errors);

        success.Should().BeFalse();
        errors.Should().ContainKey("solution");
    }

    [Fact]
    public void TryParse_SolutionRowWidthMismatch_ReturnsFalseWithRowError()
    {
        var ipuz = Parse("""
            {
              "dimensions": { "width": 3, "height": 1 },
              "puzzle": [["a","b","c"]],
              "solution": [["A","B"]]
            }
            """);

        var success = IpuzImporter.TryParse(ipuz, out _, out var errors);

        success.Should().BeFalse();
        errors.Should().ContainKey("solution[0]");
    }

    [Fact]
    public void TryParse_SolutionIsNotArray_ReturnsFalseWithSolutionError()
    {
        var ipuz = Parse("""
            {
              "dimensions": { "width": 2, "height": 1 },
              "puzzle": [["a","b"]],
              "solution": "invalid"
            }
            """);

        var success = IpuzImporter.TryParse(ipuz, out _, out var errors);

        success.Should().BeFalse();
        errors.Should().ContainKey("solution");
    }

    [Fact]
    public void TryParse_SolutionEmptyCell_ReplacedWithQuestionMark()
    {
        var ipuz = Parse("""
            {
              "dimensions": { "width": 2, "height": 1 },
              "puzzle": [["a","b"]],
              "solution": [["", "B"]]
            }
            """);

        IpuzImporter.TryParse(ipuz, out var result, out _);

        result.Solution!.SolutionGrid[0][0].Should().Be('?');
    }

    [Fact]
    public void TryParse_SolutionBlockCell_MarkedWithHash()
    {
        var ipuz = Parse("""
            {
              "dimensions": { "width": 2, "height": 1 },
              "puzzle": [["#","b"]],
              "solution": [["#","B"]]
            }
            """);

        IpuzImporter.TryParse(ipuz, out var result, out _);

        result.Solution!.SolutionGrid[0][0].Should().Be('#');
    }

    [Fact]
    public void TryParse_SolutionNumberCell_FirstDigitUsed()
    {
        var ipuz = Parse("""
            {
              "dimensions": { "width": 2, "height": 1 },
              "puzzle": [["a","b"]],
              "solution": [[1, 2]]
            }
            """);

        IpuzImporter.TryParse(ipuz, out var result, out _);

        result.Solution!.SolutionGrid[0][0].Should().Be('1');
    }
}
