using ECommerce.Api.Controllers;
using ECommerce.Application.Interfaces;
using ECommerce.SharedViewModels.DTOs.Request;
using ECommerce.SharedViewModels.DTOs.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ECommerce.Tests.Api.Controllers
{
    public class ImagesControllerTests
    {
        private readonly Mock<IImageService> _imageService;

        public ImagesControllerTests()
        {
            _imageService = new Mock<IImageService>();
        }

        private static Mock<IFormFile> CreateFormFile(string fileName, byte[] content)
        {
            var file = new Mock<IFormFile>();
            file.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(content));
            file.SetupGet(f => f.FileName).Returns(fileName);
            return file;
        }

        [Fact]
        public async Task Upload_ReturnsCreated_WithResponse()
        {
            // Arrange
            var file = CreateFormFile("pic.png", "data"u8.ToArray()).Object;
            var uploaded = new UploadImageResponse
            {
                PublicId = "pid",
                SecureUrl = "https://cdn/x.png",
                Format = "png",
                Bytes = 4,
                Width = 10,
                Height = 20,
                CreatedAt = DateTime.UtcNow,
            };
            _imageService
                .Setup(service => service.UploadAsync(It.IsAny<UploadImageRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(uploaded);
            var controller = new ImagesController(_imageService.Object);

            // Act
            var result = await controller.Upload(file, folder: null, CancellationToken.None);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(201, created.StatusCode);
            var value = Assert.IsType<UploadImageResponse>(created.Value);
            Assert.Equal("pid", value.PublicId);
            Assert.Equal("https://cdn/x.png", value.SecureUrl);
        }

        [Fact]
        public async Task Upload_PassesFileNameAndNullFolder_ToService_ByDefault()
        {
            // Arrange
            UploadImageRequest? captured = null;
            var file = CreateFormFile("a.jpg", [1, 2]).Object;
            _imageService
                .Setup(service => service.UploadAsync(It.IsAny<UploadImageRequest>(), It.IsAny<CancellationToken>()))
                .Callback<UploadImageRequest, CancellationToken>((req, _) => captured = req)
                .ReturnsAsync(new UploadImageResponse
                {
                    PublicId = "p",
                    SecureUrl = "u",
                    Format = "jpg",
                    Bytes = 1,
                    Width = 1,
                    Height = 1,
                    CreatedAt = DateTime.UtcNow,
                });
            var controller = new ImagesController(_imageService.Object);

            // Act
            await controller.Upload(file, folder: null, CancellationToken.None);

            // Assert
            Assert.NotNull(captured);
            Assert.Equal("a.jpg", captured!.FileName);
            Assert.Null(captured.Folder);
        }

        [Fact]
        public async Task Upload_PassesFolder_ToService_WhenProvided()
        {
            // Arrange
            UploadImageRequest? captured = null;
            var file = CreateFormFile("b.png", [3]).Object;
            _imageService
                .Setup(service => service.UploadAsync(It.IsAny<UploadImageRequest>(), It.IsAny<CancellationToken>()))
                .Callback<UploadImageRequest, CancellationToken>((req, _) => captured = req)
                .ReturnsAsync(new UploadImageResponse
                {
                    PublicId = "p",
                    SecureUrl = "u",
                    Format = "png",
                    Bytes = 1,
                    Width = 1,
                    Height = 1,
                    CreatedAt = DateTime.UtcNow,
                });
            var controller = new ImagesController(_imageService.Object);

            // Act
            await controller.Upload(file, folder: "products", CancellationToken.None);

            // Assert
            Assert.NotNull(captured);
            Assert.Equal("products", captured!.Folder);
        }

        [Fact]
        public async Task Upload_PassesCancellationToken_ToService()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            var token = cts.Token;
            var file = CreateFormFile("c.gif", [4]).Object;
            _imageService
                .Setup(service => service.UploadAsync(It.IsAny<UploadImageRequest>(), token))
                .ReturnsAsync(new UploadImageResponse
                {
                    PublicId = "p",
                    SecureUrl = "u",
                    Format = "gif",
                    Bytes = 1,
                    Width = 1,
                    Height = 1,
                    CreatedAt = DateTime.UtcNow,
                });
            var controller = new ImagesController(_imageService.Object);

            // Act
            await controller.Upload(file, folder: null, token);

            // Assert
            _imageService.Verify(service => service.UploadAsync(It.IsAny<UploadImageRequest>(), token), Times.Once);
            _imageService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Upload_CallsUploadAsyncOnce()
        {
            // Arrange
            var file = CreateFormFile("d.webp", [5]).Object;
            _imageService
                .Setup(service => service.UploadAsync(It.IsAny<UploadImageRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UploadImageResponse
                {
                    PublicId = "p",
                    SecureUrl = "u",
                    Format = "webp",
                    Bytes = 1,
                    Width = 1,
                    Height = 1,
                    CreatedAt = DateTime.UtcNow,
                });
            var controller = new ImagesController(_imageService.Object);

            // Act
            await controller.Upload(file, folder: null, CancellationToken.None);

            // Assert
            _imageService.Verify(service => service.UploadAsync(It.IsAny<UploadImageRequest>(), It.IsAny<CancellationToken>()), Times.Once);
            _imageService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Upload_PropagatesException_WhenServiceThrows()
        {
            // Arrange
            var file = CreateFormFile("e.png", [6]).Object;
            _imageService
                .Setup(service => service.UploadAsync(It.IsAny<UploadImageRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("storage"));
            var controller = new ImagesController(_imageService.Object);

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => controller.Upload(file, folder: null, CancellationToken.None));

            // Assert
            Assert.Equal("storage", exception.Message);
        }

        [Fact]
        public async Task Delete_ReturnsNoContent()
        {
            // Arrange
            _imageService
                .Setup(service => service.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            var controller = new ImagesController(_imageService.Object);

            // Act
            var result = await controller.Delete("https%3A%2F%2Fcdn%2Fimg.png", CancellationToken.None);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Delete_PassesImageUrlAndToken_ToService()
        {
            // Arrange
            var url = "folder%2Fimage.png";
            using var cts = new CancellationTokenSource();
            var token = cts.Token;
            _imageService
                .Setup(service => service.DeleteAsync(url, token))
                .Returns(Task.CompletedTask);
            var controller = new ImagesController(_imageService.Object);

            // Act
            await controller.Delete(url, token);

            // Assert
            _imageService.Verify(service => service.DeleteAsync(url, token), Times.Once);
            _imageService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Delete_PropagatesException_WhenServiceThrows()
        {
            // Arrange
            _imageService
                .Setup(service => service.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("missing"));
            var controller = new ImagesController(_imageService.Object);

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => controller.Delete("x", CancellationToken.None));

            // Assert
            Assert.Equal("missing", exception.Message);
        }
    }
}
