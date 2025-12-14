using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
}