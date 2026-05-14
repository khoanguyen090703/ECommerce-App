using ECommerce.Api.Controllers;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Interfaces;
using ECommerce.SharedViewModels.DTOs.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace ECommerce.Tests.Api.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<ILogger<AuthController>> _logger;
        private readonly Mock<IHttpContextAccessor> _contextAccessor;
        private readonly Mock<IAuthService> _authService;
        private readonly Mock<ICurrentUserService> _currentUserService;

        public AuthControllerTests()
        {
            _logger = new Mock<ILogger<AuthController>>();
            _contextAccessor = new Mock<IHttpContextAccessor>();
            _authService = new Mock<IAuthService>();
            _currentUserService = new Mock<ICurrentUserService>();
        }

        [Fact]
        public async Task SignUp_ReturnsOk_WhenSuccess()
        {
            // Arrange
            var request = new SignUpRequest { Email = "a@b.com", Password = "Pass@123", FullName = "A" };
            var response = new AuthResponse { IsSuccess = true, Message = "ok" };
            _authService.Setup(service => service.SignUpAsync(request)).ReturnsAsync(response);
            var controller = new AuthController(_logger.Object, _contextAccessor.Object, _authService.Object, _currentUserService.Object);

            // Act
            var result = await controller.SignUp(request);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Same(response, ok.Value);
        }

        [Fact]
        public async Task SignIn_ReturnsUnauthorized_WhenFailed()
        {
            // Arrange
            var request = new SignInRequest { Email = "a@b.com", Password = "wrong" };
            var response = new AuthResponse { IsSuccess = false, Message = "invalid" };
            _authService.Setup(service => service.SignInAsync(request)).ReturnsAsync(response);
            var controller = new AuthController(_logger.Object, _contextAccessor.Object, _authService.Object, _currentUserService.Object);

            // Act
            var result = await controller.SignIn(request);

            // Assert
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Same(response, unauthorized.Value);
        }

        [Fact]
        public async Task ConfirmEmail_ReturnsBadRequest_WhenInputInvalid()
        {
            // Arrange
            var controller = new AuthController(_logger.Object, _contextAccessor.Object, _authService.Object, _currentUserService.Object);

            // Act
            var result = await controller.ConfirmEmail("", "");

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
            _authService.Verify(service => service.ConfirmEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RefreshToken_ReturnsBadRequest_WhenTokenMissing()
        {
            // Arrange
            var request = new RefreshTokenRequest { AccessToken = "", RefreshToken = "" };
            var controller = new AuthController(_logger.Object, _contextAccessor.Object, _authService.Object, _currentUserService.Object);

            // Act
            var result = await controller.RefreshToken(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
            _authService.Verify(service => service.RefreshTokenAsync(It.IsAny<RefreshTokenRequest>()), Times.Never);
        }

        [Fact]
        public async Task Logout_ReturnsUnauthorized_WhenCurrentUserMissing()
        {
            // Arrange
            _currentUserService.SetupGet(service => service.UserId).Returns((Guid?)null);
            var controller = new AuthController(_logger.Object, _contextAccessor.Object, _authService.Object, _currentUserService.Object);

            // Act
            var result = await controller.Logout();

            // Assert
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            var payload = Assert.IsType<AuthResponse>(unauthorized.Value);
            Assert.False(payload.IsSuccess);
        }

        [Fact]
        public async Task Logout_ReturnsOk_WhenSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var response = new AuthResponse { IsSuccess = true, Message = "logged out" };
            _currentUserService.SetupGet(service => service.UserId).Returns(userId);
            _authService.Setup(service => service.LogoutAsync(userId.ToString())).ReturnsAsync(response);
            var controller = new AuthController(_logger.Object, _contextAccessor.Object, _authService.Object, _currentUserService.Object);

            // Act
            var result = await controller.Logout();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Same(response, ok.Value);
        }

        [Fact]
        public async Task ResendConfirmation_ReturnsBadRequest_WhenEmailEmpty()
        {
            // Arrange
            var request = new ResendEmailConfirmationRequest { Email = "" };
            var controller = new AuthController(_logger.Object, _contextAccessor.Object, _authService.Object, _currentUserService.Object);

            // Act
            var result = await controller.ResendConfirmation(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
