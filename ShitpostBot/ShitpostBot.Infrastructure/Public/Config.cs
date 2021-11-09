using JsonSubTypes;
using Newtonsoft.Json;
using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure
{
    public static class Config
    {
        public static readonly JsonSerializerSettings DatabaseJsonSerializerSettings = new JsonSerializerSettings
        {
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            ContractResolver = new PrivatePropertyResolver(),
            Converters =
            {
                JsonSubtypesConverterBuilder
                    .Of(typeof(PostContent), nameof(PostContent.Type))
                    .RegisterSubtype<ImagePostContent>(PostType.Image)
                    .RegisterSubtype<LinkPostContent>(PostType.Link)
                    .Build()
            }
        };
    }
}