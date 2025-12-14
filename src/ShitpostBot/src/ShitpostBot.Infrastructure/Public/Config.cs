using System.Text.Json;

namespace ShitpostBot.Infrastructure;

public static class Config
{
    public static readonly JsonSerializerOptions DatabaseJsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };
}