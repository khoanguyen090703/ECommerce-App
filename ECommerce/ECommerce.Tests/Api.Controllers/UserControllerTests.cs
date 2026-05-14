using ECommerce.Api.Controllers;
using ECommerce.Application.Interfaces;
using ECommerce.SharedViewModels.DTOs.Response;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ECommerce.Tests.Api.Controllers
{
    public class UserControllerTests
    {
        private readonly Mock<IUserService> _userService;

        public UserControllerTests()
        {
            _userService = new Mock<IUserService>();
        }

        [Fact]
        public async Task GetMe_ReturnsOk_WithProfile()
        {
            // Arrange
            var profile = new UserProfileResponse
            {
                Id = "u1",
                FullName = "Test User",
                Email = "t@x.com",
                AvatarUrl = null,
            };
            _userService
                .Setup(service => service.GetMyProfileAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);
            var controller = new UserController(_userService.Object);

            // Act
            var result = await controller.GetMe(CancellationToken.None);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var value = Assert.IsType<UserProfileResponse>(ok.Value);
            Assert.Equal("u1", value.Id);
            Assert.Equal("Test User", value.FullName);
            Assert.Equal("t@x.com", value.Email);
        }

        [Fact]
        public async Task GetMe_PassesCancellationToken_ToService()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            var token = cts.Token;
            _userService
                .Setup(service => service.GetMyProfileAsync(token))
                .ReturnsAsync(new UserProfileResponse { Id = "x", FullName = "A", Email = "a@b.c" });
            var controller = new UserController(_userService.Object);

            // Act
            await controller.GetMe(token);

            // Assert
            _userService.Verify(service => service.GetMyProfileAsync(token), Times.Once);
            _userService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetMe_CallsGetMyProfileAsyncOnce()
        {
            // Arrange
            _userService
                .Setup(service => service.GetMyProfileAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserProfileResponse { Id = "x", FullName = "A", Email = "a@b.c" });
            var controller = new UserController(_userService.Object);

            // Act
            await controller.GetMe(CancellationToken.None);

            // Assert
            _userService.Verify(service => service.GetMyProfileAsync(It.IsAny<CancellationToken>()), Times.Once);
            _userService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetMe_PropagatesException_WhenServiceThrows()
        {
            // Arrange
            _userService
                .Setup(service => service.GetMyProfileAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("db"));
            var controller = new UserController(_userService.Object);

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => controller.GetMe(CancellationToken.None));

            // Assert
            Assert.Equal("db", exception.Message);
        }
    }
}
