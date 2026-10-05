using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure;

internal sealed class DailySlopScoreConverter()
    : ValueConverter<DailySlopScore?, string>(
        score => JsonSerializer.Serialize(score, SerializerOptions),
        json => JsonSerializer.Deserialize<DailySlopScore>(json, SerializerOptions)
    )
{
    private static readonly JsonSerializerOptions SerializerOptions = new(
        JsonSerializerDefaults.Web
    )
    {
        RespectRequiredConstructorParameters = true,
        // PostgreSQL jsonb can reorder fields, including the type discriminator.
        AllowOutOfOrderMetadataProperties = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers =
            {
                typeInfo =>
                {
                    if (typeInfo.Type == typeof(DailySlopScore))
                    {
                        typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
                        {
                            TypeDiscriminatorPropertyName = "type",
                            DerivedTypes = { new JsonDerivedType(typeof(RngdleScore), "rngdle") },
                        };
                    }
                },
            },
        },
    };
}
