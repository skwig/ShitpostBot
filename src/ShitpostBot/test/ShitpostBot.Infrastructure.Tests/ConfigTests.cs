using System.Text.Json;
using NUnit.Framework;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Infrastructure.Tests;

[TestFixture]
public class ConfigTests
{
    [Test]
    public void Config_ShouldProvideSystemTextJsonOptions()
    {
        var options = Config.DatabaseJsonSerializerOptions;
        
        Assert.That(options, Is.Not.Null);
        Assert.That(options.PropertyNamingPolicy, Is.EqualTo(JsonNamingPolicy.CamelCase));
    }
}