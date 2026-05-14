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
    public class ProductControllerTests
    {
        private readonly Mock<ILogger<ProductController>> _logger;
        private readonly Mock<IProductService> _productService;

        public ProductControllerTests()
        {
            _logger = new Mock<ILogger<ProductController>>();
            _productService = new Mock<IProductService>();
        }

        [Fact]
        public async Task GetAll_ReturnsOk_WithProducts()
        {
            // Arrange
            _productService.Setup(service => service.GetAllAsync()).ReturnsAsync(new List<ProductResponse4List>());
            var controller = new ProductController(_logger.Object, _productService.Object);

            // Act
            var result = await controller.GetAll();

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Create_ReturnsCreated_AndCallsService()
        {
            // Arrange
            var request = new CreateProductRequest { Description = "Fresh", BrandId = 1 };
            _productService.Setup(service => service.AddAsync(request)).Returns(Task.CompletedTask);
            var controller = new ProductController(_logger.Object, _productService.Object);

            // Act
            var result = await controller.Create(request);

            // Assert
            Assert.IsType<CreatedResult>(result);
            _productService.Verify(service => service.AddAsync(request), Times.Once);
        }

        [Fact]
        public async Task GetById_ReturnsBadRequest_WhenIdInvalid()
        {
            // Arrange
            var controller = new ProductController(_logger.Object, _productService.Object);

            // Act
            var result = await controller.GetById(0, includeVariants: true);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
            _productService.Verify(service => service.GetProductByIdAsync(It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task GetById_PassesIncludeVariants_ToService()
        {
            // Arrange
            var response = new ProductDetailsResponse { Id = 10, Name = "Dior" };
            _productService.Setup(service => service.GetProductByIdAsync(10, false)).ReturnsAsync(response);
            var controller = new ProductController(_logger.Object, _productService.Object);

            // Act
            var result = await controller.GetById(10, includeVariants: false);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.IsType<ProductDetailsResponse>(ok.Value);
            _productService.Verify(service => service.GetProductByIdAsync(10, false), Times.Once);
        }

        [Fact]
        public async Task GetProductVariants_ReturnsBadRequest_WhenProductIdInvalid()
        {
            // Arrange
            var controller = new ProductController(_logger.Object, _productService.Object);

            // Act
            var result = await controller.GetProductVariants(0, new ProductVariantsQueryParams());

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateStatus_ReturnsNoContent_AndCallsService()
        {
            // Arrange
            var request = new UpdateProductStatusRequest { Status = ProductStatus.Active };
            _productService.Setup(service => service.UpdateProductStatusAsync(2, ProductStatus.Active)).Returns(Task.CompletedTask);
            var controller = new ProductController(_logger.Object, _productService.Object);

            // Act
            var result = await controller.UpdateStatus(2, request);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _productService.Verify(service => service.UpdateProductStatusAsync(2, ProductStatus.Active), Times.Once);
        }

        [Fact]
        public async Task Get_ReturnsOk_WithPagedProducts()
        {
            // Arrange
            var parameters = new ProductQueryParams { SearchTerm = "bleu" };
            var paged = new PagedResult<ProductResponse4List>(new List<ProductResponse4List>(), 0, 1, 10);
            _productService.Setup(service => service.GetProductsAsync(parameters)).ReturnsAsync(paged);
            var controller = new ProductController(_logger.Object, _productService.Object);

            // Act
            var result = await controller.Get(parameters);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.IsType<PagedResult<ProductResponse4List>>(ok.Value);
        }
    }
}
