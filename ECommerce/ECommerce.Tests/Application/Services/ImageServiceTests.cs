using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.SharedViewModels.DTOs.Request;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ECommerce.Tests.Application.Services;

public class ImageServiceTests
{
    [Fact]
    public async Task UploadAsync_WhenFileExtensionNotAllowed_ThrowsArgumentException()
    {
        // Arrange
        var storage = new Mock<IImageStorageService>();
        var sut = new ImageService(storage.Object, NullLogger<ImageService>.Instance);
        await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var request = new UploadImageRequest
        {
            FileStream = stream,
            FileName = "photo.exe"
        };

        // Act
        var act = async () => await sut.UploadAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*not allowed*");
        storage.Verify(
            s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UploadAsync_WhenFileExceedsMaxSize_ThrowsArgumentException()
    {
        // Arrange
        var storage = new Mock<IImageStorageService>();
        var sut = new ImageService(storage.Object, NullLogger<ImageService>.Instance);
        await using var stream = new MemoryStream();
        stream.SetLength(10L * 1024 * 1024 + 1);
        var request = new UploadImageRequest
        {
            FileStream = stream,
            FileName = "big.jpg"
        };

        // Act
        var act = async () => await sut.UploadAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*maximum*");
        storage.Verify(
            s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UploadAsync_WhenStreamEmpty_ThrowsArgumentException()
    {
        // Arrange
        var storage = new Mock<IImageStorageService>();
        var sut = new ImageService(storage.Object, NullLogger<ImageService>.Instance);
        await using var stream = new MemoryStream();
        var request = new UploadImageRequest
        {
            FileStream = stream,
            FileName = "a.jpg"
        };

        // Act
        var act = async () => await sut.UploadAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*empty*");
    }

    [Fact]
    public async Task UploadAsync_WhenValid_DelegatesToStorageAndReturnsResponse()
    {
        // Arrange
        await using var input = new MemoryStream(new byte[1024]);
        var request = new UploadImageRequest
        {
            FileStream = input,
            FileName = "pic.png",
            Folder = "products"
        };
        var uploaded = UploadedImage.Create("pid", "https://cdn/x.png", "png", 1024, 100, 200);
        var storage = new Mock<IImageStorageService>();
        storage
            .Setup(s => s.UploadAsync(input, "pic.png", "products", It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploaded);
        var sut = new ImageService(storage.Object, NullLogger<ImageService>.Instance);

        // Act
        var result = await sut.UploadAsync(request, CancellationToken.None);

        // Assert
        result.PublicId.Should().Be("pid");
        result.SecureUrl.Should().Be("https://cdn/x.png");
        result.Bytes.Should().Be(1024);
    }

    [Fact]
    public async Task DeleteAsync_WhenUrlNull_ThrowsArgumentException()
    {
        // Arrange
        var storage = new Mock<IImageStorageService>();
        var sut = new ImageService(storage.Object, NullLogger<ImageService>.Instance);

        // Act
        var act = async () => await sut.DeleteAsync("  ", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        storage.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenValid_CallsStorage()
    {
        // Arrange
        var storage = new Mock<IImageStorageService>();
        storage.Setup(s => s.DeleteAsync("https://x/a.png", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = new ImageService(storage.Object, NullLogger<ImageService>.Instance);

        // Act
        await sut.DeleteAsync("https://x/a.png", CancellationToken.None);

        // Assert
        storage.Verify(s => s.DeleteAsync("https://x/a.png", It.IsAny<CancellationToken>()), Times.Once);
    }
}
