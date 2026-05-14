using ECommerce.Api.Controllers;
using ECommerce.Application.Services;
using ECommerce.SharedViewModels.DTOs.Response;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ECommerce.Tests.Api.Controllers
{
    public class PaymentMethodControllerTests
    {
        private readonly Mock<IPaymentMethodService> _service;

        public PaymentMethodControllerTests()
        {
            _service = new Mock<IPaymentMethodService>();
        }

        [Fact]
        public async Task GetAll_ReturnsOk_WithEmptyList_WhenIncludeInactiveFalseByDefault()
        {
            // Arrange
            _service.Setup(s => s.GetAllAsync(false)).ReturnsAsync(new List<PaymentMethodResponse>());
            var controller = new PaymentMethodController(_service.Object);

            // Act
            var result = await controller.GetAll();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var value = Assert.IsAssignableFrom<IReadOnlyList<PaymentMethodResponse>>(ok.Value);
            Assert.Empty(value);
        }

        [Fact]
        public async Task GetAll_CallsGetAllAsyncWithFalse_WhenIncludeInactiveOmitted()
        {
            // Arrange
            _service.Setup(s => s.GetAllAsync(false)).ReturnsAsync(new List<PaymentMethodResponse>());
            var controller = new PaymentMethodController(_service.Object);

            // Act
            await controller.GetAll();

            // Assert
            _service.Verify(s => s.GetAllAsync(false), Times.Once);
            _service.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetAll_ReturnsOk_WithPaymentMethods_WhenIncludeInactiveFalse()
        {
            // Arrange
            var list = new List<PaymentMethodResponse>
            {
                new PaymentMethodResponse { Id = 1, Name = "COD" },
                new PaymentMethodResponse { Id = 2, Name = "Bank" },
            };
            _service.Setup(s => s.GetAllAsync(false)).ReturnsAsync(list);
            var controller = new PaymentMethodController(_service.Object);

            // Act
            var result = await controller.GetAll(includeInactive: false);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var value = Assert.IsAssignableFrom<List<PaymentMethodResponse>>(ok.Value);
            Assert.Equal(2, value.Count);
            Assert.Equal("COD", value[0].Name);
        }

        [Fact]
        public async Task GetAll_CallsGetAllAsyncWithTrue_WhenIncludeInactiveTrue()
        {
            // Arrange
            _service.Setup(s => s.GetAllAsync(true)).ReturnsAsync(new List<PaymentMethodResponse>());
            var controller = new PaymentMethodController(_service.Object);

            // Act
            await controller.GetAll(includeInactive: true);

            // Assert
            _service.Verify(s => s.GetAllAsync(true), Times.Once);
            _service.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetAll_ReturnsOk_WithPaymentMethods_WhenIncludeInactiveTrue()
        {
            // Arrange
            var list = new List<PaymentMethodResponse>
            {
                new PaymentMethodResponse { Id = 1, Name = "Active" },
            };
            _service.Setup(s => s.GetAllAsync(true)).ReturnsAsync(list);
            var controller = new PaymentMethodController(_service.Object);

            // Act
            var result = await controller.GetAll(includeInactive: true);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var value = Assert.IsAssignableFrom<List<PaymentMethodResponse>>(ok.Value);
            Assert.Single(value);
            Assert.Equal("Active", value[0].Name);
        }

        [Fact]
        public async Task GetAll_PropagatesException_WhenServiceThrows_WithIncludeInactiveFalse()
        {
            // Arrange
            _service.Setup(s => s.GetAllAsync(false)).ThrowsAsync(new InvalidOperationException("db"));
            var controller = new PaymentMethodController(_service.Object);

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => controller.GetAll());

            // Assert
            Assert.Equal("db", exception.Message);
        }

        [Fact]
        public async Task GetAll_PropagatesException_WhenServiceThrows_WithIncludeInactiveTrue()
        {
            // Arrange
            _service.Setup(s => s.GetAllAsync(true)).ThrowsAsync(new InvalidOperationException("db"));
            var controller = new PaymentMethodController(_service.Object);

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => controller.GetAll(includeInactive: true));

            // Assert
            Assert.Equal("db", exception.Message);
        }
    }
}
