using FluentAssertions;
using ShitpostBot.Domain;
using Xunit;

namespace ShitpostBot.Tests.Unit;

public class ImagePostTests
{
    [Fact]
    public void MarkPostAsUnavailable_SetsIsPostAvailableToFalse()
    {
        // Arrange
        var imagePost = CreateTestImagePost();
        imagePost.IsPostAvailable.Should().BeTrue();

        // Act
        imagePost.MarkPostAsUnavailable();

        // Assert
        imagePost.IsPostAvailable.Should().BeFalse();
    }

    [Fact]
    public void MarkPostAsUnavailable_DoesNotUpdateEvaluatedOn()
    {
        // Arrange
        var imagePost = CreateTestImagePost();
        var originalEvaluatedOn = imagePost.EvaluatedOn;

        // Act
        imagePost.MarkPostAsUnavailable();

        // Assert
        imagePost.EvaluatedOn.Should().Be(originalEvaluatedOn);
    }

    [Fact]
    public void RefreshImageUrl_UpdatesImageUri()
    {
        // Arrange
        var imagePost = CreateTestImagePost();
        var oldUri = imagePost.Image.ImageUri;
        var newUri = new Uri("https://cdn.discordapp.com/attachments/123/456/new-image.png");

        // Act
        imagePost.RefreshImageUrl(newUri, null);

        // Assert
        imagePost.Image.ImageUri.Should().Be(newUri);
        imagePost.Image.ImageUri.Should().NotBe(oldUri);
    }

    [Fact]
    public void RefreshImageUrl_SetsIsPostAvailableToTrue()
    {
        // Arrange
        var imagePost = CreateTestImagePost();
        imagePost.MarkPostAsUnavailable();
        imagePost.IsPostAvailable.Should().BeFalse();

        var newUri = new Uri("https://cdn.discordapp.com/attachments/123/456/new-image.png");

        // Act
        imagePost.RefreshImageUrl(newUri, null);

        // Assert
        imagePost.IsPostAvailable.Should().BeTrue();
    }

    [Fact]
    public void RefreshImageUrl_PreservesImageFeatures()
    {
        // Arrange
        var imagePost = CreateTestImagePost();
        var features = new ImageFeatures("test-model", new Pgvector.Vector(new float[] { 1.0f, 2.0f, 3.0f }));
        imagePost.SetImageFeatures(features, DateTimeOffset.UtcNow);
        
        var originalFeatures = imagePost.Image.ImageFeatures;
        var newUri = new Uri("https://cdn.discordapp.com/attachments/123/456/new-image.png");

        // Act
        imagePost.RefreshImageUrl(newUri, null);

        // Assert
        imagePost.Image.ImageFeatures.Should().Be(originalFeatures);
    }

    [Fact]
    public void RefreshImageUrl_PreservesImageId()
    {
        // Arrange
        var imagePost = CreateTestImagePost();
        var originalImageId = imagePost.Image.ImageId;
        var newUri = new Uri("https://cdn.discordapp.com/attachments/123/456/new-image.png");

        // Act
        imagePost.RefreshImageUrl(newUri, null);

        // Assert
        imagePost.Image.ImageId.Should().Be(originalImageId);
    }

    private static ImagePost CreateTestImagePost()
    {
        var image = Image.CreateOrDefault(
            12345ul,
            new Uri("https://cdn.discordapp.com/attachments/123/456/old-image.png"),
            "image/png"
        )!;

        return ImagePost.Create(
            DateTimeOffset.UtcNow,
            new ChatMessageIdentifier(1, 2, 3),
            new PosterIdentifier(100),
            DateTimeOffset.UtcNow,
            image
        );
    }
}
