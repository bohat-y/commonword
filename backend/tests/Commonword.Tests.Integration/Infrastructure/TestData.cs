namespace Commonword.Tests.Integration.Infrastructure;

public static class TestData
{
    public const string AdminKey = "test-admin-key";

    // Minimal valid 3x3 ipuz with a 3-letter across word and solution
    public const string SimpleIpuz = """
        {
          "title": "Test Puzzle",
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
            ["C", "A", "T"],
            ["#", "P", "E"],
            ["D", "O", "G"]
          ]
        }
        """;

    public const string InvalidIpuz = """{"title": "Bad"}""";
}
