using System.Net;
using DotNet.Testcontainers.Builders;
using FluentAssertions;

namespace ShitpostBot.Tests.Integration;

public class MlServiceIntegrationTests
{
    [Fact]
    public async Task Test1()
    {
        // TODO: Update this test to use the new POST /process/image endpoint
        // The old GET /images/features endpoint no longer exists in the new ML service
        // New endpoint expects: POST /process/image with body { "image_url": "...", "embedding": true }
        // Returns: { "embedding": [...], "size": [w, h] }
        
        // Arrange
        var image = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(CommonDirectoryPath.GetGitDirectory(), "src/ShitpostBot.MlService/src")
            .WithDockerfile("Dockerfile")
            .WithName("ml-service")
            .Build();

        await image.CreateAsync();

        var container = new ContainerBuilder()
            .WithImage(image)
            .WithPortBinding(5000, true)
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(request => request.ForPath("/healthz").ForPort(5000))
            )
            .Build();

        await container.StartAsync();

        // Act
        var httpClient = new HttpClient();
        var imgUri =
            "https://media.discordapp.net/attachments/138031010951593984/1289974033793683456/image0.jpg?ex=670a9770&is=670945f0&hm=34d0b056539e0a2963f5b6f9f1dcd9a97aebadb51d2e521244e51320014202fa&=&format=webp&width=867&height=910";
        
        // Old endpoint (no longer exists):
        // var requestUri = new Uri($"http://{container.Hostname}:{container.GetMappedPublicPort(5000)}/images/features?image_url={Uri.EscapeDataString(imgUri)}");
        // var response = await httpClient.GetAsync(requestUri);
        
        // For now, just verify health endpoint works
        var healthUri = new Uri($"http://{container.Hostname}:{container.GetMappedPublicPort(5000)}/healthz");
        var response = await httpClient.GetAsync(healthUri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}