using ECommerce.Api.Controllers;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;
using ECommerce.Domain.QueryParameters;
using ECommerce.SharedViewModels.DTOs.Request;
using ECommerce.SharedViewModels.DTOs.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace ECommerce.Tests.Api.Controllers
{
    public class VariantControllerTests
    {
        private readonly Mock<ILogger<VariantController>> _logger;
        private readonly Mock<IVariantService> _variantService;

        public VariantControllerTests()
        {
            _logger = new Mock<ILogger<VariantController>>();
            _variantService = new Mock<IVariantService>();
        }

        [Fact]
        public async Task GetAll_ReturnsOk_WithItems()
        {
            // Arrange
            _variantService.Setup(service => service.GetAllVariantsAsync()).ReturnsAsync(new List<VariantResponse>());
            var controller = new VariantController(_logger.Object, _variantService.Object);

            // Act
            var result = await controller.GetAll();

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetRestockVariants_ReturnsOk_WithPagedResult()
        {
            // Arrange
            var parameters = new RestockVariantQueryParams();
            var paged = new PagedResult<VariantStockPanelResponse>(new List<VariantStockPanelResponse>(), 0, 1, 10);
            _variantService.Setup(service => service.GetVariantsForStockRestockAsync(parameters)).ReturnsAsync(paged);
            var controller = new VariantController(_logger.Object, _variantService.Object);

            // Act
            var result = await controller.GetRestockVariants(parameters);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.IsType<PagedResult<VariantStockPanelResponse>>(ok.Value);
        }

        [Fact]
        public async Task GetById_ReturnsBadRequest_WhenIdInvalid()
        {
            // Arrange
            var controller = new VariantController(_logger.Object, _variantService.Object);

            // Act
            var result = await controller.GetById(0);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenServiceReturnsNull()
        {
            // Arrange
            _variantService.Setup(service => service.GetVariantDetailsByIdAsync(11)).ReturnsAsync((ProductVariantDetailsResponse?)null);
            var controller = new VariantController(_logger.Object, _variantService.Object);

            // Act
            var result = await controller.GetById(11);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Create_ReturnsBadRequest_WhenProductIdInvalid()
        {
            // Arrange
            var controller = new VariantController(_logger.Object, _variantService.Object);

            // Act
            var result = await controller.Create(0, new CreateVariantRequest { StockQuantity = 1 });

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Create_ReturnsCreatedAtAction_WhenSuccess()
        {
            // Arrange
            var request = new CreateVariantRequest { StockQuantity = 1 };
            _variantService.Setup(service => service.CreateVariantAsync(5, request)).ReturnsAsync(99);
            var controller = new VariantController(_logger.Object, _variantService.Object);

            // Act
            var result = await controller.Create(5, request);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal("GetById", created.ActionName);
            Assert.Equal(99, created.RouteValues?["id"]);
        }

        [Fact]
        public async Task SetFeatured_ReturnsBadRequest_WhenIdsMissing()
        {
            // Arrange
            var controller = new VariantController(_logger.Object, _variantService.Object);

            // Act
            var result = await controller.SetFeatured(new List<int>());

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task PostRestock_ReturnsNoContent_AndCallsService()
        {
            // Arrange
            var request = new AddVariantStockBatchRequest
            {
                Items = new List<AddVariantStockLineRequest> { new AddVariantStockLineRequest { VariantId = 1, QuantityToAdd = 2 } }
            };
            _variantService.Setup(service => service.AddStockToVariantsAsync(request)).Returns(Task.CompletedTask);
            var controller = new VariantController(_logger.Object, _variantService.Object);

            // Act
            var result = await controller.PostRestock(request);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _variantService.Verify(service => service.AddStockToVariantsAsync(request), Times.Once);
        }

        [Fact]
        public async Task UpdateStatus_ReturnsNoContent_AndCallsService()
        {
            // Arrange
            var request = new UpdateVariantStatusRequest { Status = VariantStatus.Available };
            _variantService.Setup(service => service.UpdateVariantStatusByIdAsync(7, VariantStatus.Available)).Returns(Task.CompletedTask);
            var controller = new VariantController(_logger.Object, _variantService.Object);

            // Act
            var result = await controller.UpdateStatus(7, request);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _variantService.Verify(service => service.UpdateVariantStatusByIdAsync(7, VariantStatus.Available), Times.Once);
        }

        [Fact]
        public async Task Delete_ReturnsBadRequest_WhenIdInvalid()
        {
            // Arrange
            var controller = new VariantController(_logger.Object, _variantService.Object);

            // Act
            var result = await controller.Delete(0);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
