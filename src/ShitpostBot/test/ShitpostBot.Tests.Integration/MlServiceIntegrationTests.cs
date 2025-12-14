using System.Net;
using DotNet.Testcontainers.Builders;
using FluentAssertions;

namespace ShitpostBot.Tests.Integration;

public class MlServiceIntegrationTests
{
    [Fact]
    public async Task Test1()
    {
        // Arrange
        var image = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(CommonDirectoryPath.GetGitDirectory(), "src/ShitpostBot.MlService")
            .WithDockerfile("Dockerfile")
            .WithName("ml-service")
            .Build();

        await image.CreateAsync();

        var container = new ContainerBuilder()
            .WithImage(image)
            .WithPortBinding(5000, true)
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(request => request.ForPath("/").ForPort(5000))
            )
            .Build();

        await container.StartAsync();

        // Act
        var httpClient = new HttpClient();
        var imgUri =
            "https://media.discordapp.net/attachments/138031010951593984/1289974033793683456/image0.jpg?ex=670a9770&is=670945f0&hm=34d0b056539e0a2963f5b6f9f1dcd9a97aebadb51d2e521244e51320014202fa&=&format=webp&width=867&height=910";
        var requestUri = new Uri(
            $"http://{container.Hostname}:{container.GetMappedPublicPort(5000)}/images/features?image_url={Uri.EscapeDataString(imgUri)}");
        var response = await httpClient.GetAsync(requestUri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}