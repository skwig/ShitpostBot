using System.Net;
using System.Net.Http.Json;
using DotNet.Testcontainers.Builders;
using FluentAssertions;

namespace ShitpostBot.Tests.Integration;

public class MlServiceIntegrationTests
{
    [Fact]
    public async Task WhiteTextOnBlackBackground()
    {
        // Arrange
        var image = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(CommonDirectoryPath.GetGitDirectory(), "ShitpostBot.MlService")
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
        var imgUrl =
            "https://media.discordapp.net/attachments/170866439794851841/1294400656425357432/invert.jpg?ex=674c224d&is=674ad0cd&hm=a63c71523b3500b47986549a0bb67ac2044add544a8351719b2f69d41b519c29&=&format=webp&width=861&height=903";
        var requestUri = new Uri(
            $"http://{container.Hostname}:{container.GetMappedPublicPort(5000)}/images/features?image_url={Uri.EscapeDataString(imgUrl)}");
        var response = await httpClient.GetAsync(requestUri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<Response>()!;
        json.image_features.Should().HaveCount(1536);
        json.image_url.ToString().Should().Be(imgUrl);
        json.text_lines.Should().BeEquivalentTo("europeans when", "someone smiles instead", "of skilometres");
    }
}

public record Response(double[] image_features, Uri image_url, string[] text_lines);