using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NUnit.Framework;
using ShitpostBot.Infrastructure;
using ShitpostBot.Domain;

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

    [Test]
    public void PostConfiguration_ShouldConfigureCorrectly()
    {
        // Test that Post configuration doesn't depend on Newtonsoft.Json
        var builder = new ModelBuilder();
        var configuration = new PostConfiguration();
        
        Assert.DoesNotThrow(() => configuration.Configure(builder.Entity<Post>()));
    }
}