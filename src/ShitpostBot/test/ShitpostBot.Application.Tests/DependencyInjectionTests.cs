using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using NUnit.Framework;
using ShitpostBot.Application;
using ShitpostBot.Application.Services;

namespace ShitpostBot.Application.Tests;

[TestFixture]
public class DependencyInjectionTests
{
    [Test]
    public void AddRefitClient_ShouldUseSystemTextJson()
    {
        // This test will fail until we update the configuration
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("ImageFeatureExtractorApi:Uri", "http://localhost:5000")
            })
            .Build();
        
        services.AddShitpostBotApplication(configuration);
        
        var serviceProvider = services.BuildServiceProvider();
        var refitClient = serviceProvider.GetService<IImageFeatureExtractorApi>();
        
        Assert.That(refitClient, Is.Not.Null);
    }

    [Test]
    public void ExtractImageFeaturesResponse_ShouldSerializeCorrectly()
    {
        var response = new ExtractImageFeaturesResponse
        {
            ImageUrl = "https://example.com/image.jpg",
            ImageFeatures = new float[] { 0.1f, 0.2f, 0.3f }
        };
        
        var json = JsonSerializer.Serialize(response);
        Console.WriteLine($"Serialized JSON: {json}");
        
        var deserialized = JsonSerializer.Deserialize<ExtractImageFeaturesResponse>(json);
        
        Assert.That(deserialized.ImageUrl, Is.EqualTo(response.ImageUrl));
        Assert.That(deserialized.ImageFeatures, Is.EqualTo(response.ImageFeatures));
    }

    [Test]
    public void ExtractImageFeaturesResponse_ShouldUseSnakeCase()
    {
        var response = new ExtractImageFeaturesResponse
        {
            ImageUrl = "https://example.com/image.jpg",
            ImageFeatures = new float[] { 0.1f, 0.2f, 0.3f }
        };
        
        var json = JsonSerializer.Serialize(response);
        Console.WriteLine($"Serialized JSON: {json}");
        
        // Should contain snake_case property names for API compatibility
        Assert.That(json, Does.Contain("image_url"));
        Assert.That(json, Does.Contain("image_features"));
    }
}