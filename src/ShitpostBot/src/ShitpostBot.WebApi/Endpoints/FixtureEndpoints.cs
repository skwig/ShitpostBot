using System.IO;
using Microsoft.AspNetCore.Mvc;

namespace ShitpostBot.WebApi.Endpoints;

public static class FixtureEndpoints
{
    public static void MapFixtureEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/test")
            .WithTags("Test");

        group.MapGet("/fixtures", GetFixtures);
    }

    private static IResult GetFixtures()
    {
        var fixturesPath = Path.Combine(Directory.GetCurrentDirectory(), "fixtures", "images");
        
        if (!Directory.Exists(fixturesPath))
        {
            return Results.Ok(new FixturesResponse
            {
                Reposts = Array.Empty<string>(),
                NonReposts = Array.Empty<string>(),
                EdgeCases = Array.Empty<string>()
            });
        }

        var response = new FixturesResponse
        {
            Reposts = GetFilesInDirectory(Path.Combine(fixturesPath, "reposts")),
            NonReposts = GetFilesInDirectory(Path.Combine(fixturesPath, "non-reposts")),
            EdgeCases = GetFilesInDirectory(Path.Combine(fixturesPath, "edge-cases"))
        };

        return Results.Ok(response);
    }

    private static string[] GetFilesInDirectory(string path)
    {
        if (!Directory.Exists(path))
            return Array.Empty<string>();

        return Directory.GetFiles(path)
            .Select(Path.GetFileName)
            .Where(name => name != null && !name.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            .Cast<string>()
            .ToArray();
    }
}

public record FixturesResponse
{
    public required string[] Reposts { get; init; }
    public required string[] NonReposts { get; init; }
    public required string[] EdgeCases { get; init; }
}