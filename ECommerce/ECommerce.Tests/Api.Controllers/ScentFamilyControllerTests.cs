using ECommerce.Api.Controllers;
using ECommerce.Application.Interfaces;
using ECommerce.SharedViewModels.DTOs.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace ECommerce.Tests.Api.Controllers
{
    public class ScentFamilyControllerTests
    {
        private readonly Mock<ILogger<ScentFamilyController>> _logger;
        private readonly Mock<IScentFamilyService> _scentFamilyService;

        public ScentFamilyControllerTests()
        {
            _logger = new Mock<ILogger<ScentFamilyController>>();
            _scentFamilyService = new Mock<IScentFamilyService>();
        }

        [Fact]
        public async Task GetAll_ReturnsOk_WithEmptyList_WhenNoItems()
        {
            // Arrange
            _scentFamilyService
                .Setup(service => service.GetAllScentFamiliesAsync())
                .ReturnsAsync(new List<ScentFamilyResponse>());
            var controller = new ScentFamilyController(_logger.Object, _scentFamilyService.Object);

            // Act
            var result = await controller.GetAll();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var value = Assert.IsAssignableFrom<IReadOnlyList<ScentFamilyResponse>>(ok.Value);
            Assert.Empty(value);
        }

        [Fact]
        public async Task GetAll_ReturnsOk_WithScentFamilies()
        {
            // Arrange
            var items = new List<ScentFamilyResponse>
            {
                new ScentFamilyResponse { Id = 1, Name = "Floral", Description = "Hoa" },
                new ScentFamilyResponse { Id = 2, Name = "Woody", Description = null },
            };
            _scentFamilyService.Setup(service => service.GetAllScentFamiliesAsync()).ReturnsAsync(items);
            var controller = new ScentFamilyController(_logger.Object, _scentFamilyService.Object);

            // Act
            var result = await controller.GetAll();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var value = Assert.IsAssignableFrom<List<ScentFamilyResponse>>(ok.Value);
            Assert.Equal(2, value.Count);
            Assert.Equal("Floral", value[0].Name);
            Assert.Equal("Woody", value[1].Name);
        }

        [Fact]
        public async Task GetAll_CallsGetAllScentFamiliesAsyncOnce()
        {
            // Arrange
            _scentFamilyService
                .Setup(service => service.GetAllScentFamiliesAsync())
                .ReturnsAsync(new List<ScentFamilyResponse>());
            var controller = new ScentFamilyController(_logger.Object, _scentFamilyService.Object);

            // Act
            await controller.GetAll();

            // Assert
            _scentFamilyService.Verify(service => service.GetAllScentFamiliesAsync(), Times.Once);
            _scentFamilyService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetAll_PropagatesException_WhenServiceThrows()
        {
            // Arrange
            _scentFamilyService
                .Setup(service => service.GetAllScentFamiliesAsync())
                .ThrowsAsync(new InvalidOperationException("db"));
            var controller = new ScentFamilyController(_logger.Object, _scentFamilyService.Object);

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => controller.GetAll());

            // Assert
            Assert.Equal("db", exception.Message);
        }
    }
}
