using Newtonsoft.Json;

namespace ShitpostBot.Infrastructure
{
    public static class Config
    {
        public static readonly JsonSerializerSettings DatabaseJsonSerializerSettings = new JsonSerializerSettings
        {
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            ContractResolver = new PrivatePropertyResolver()
        };
    }
}