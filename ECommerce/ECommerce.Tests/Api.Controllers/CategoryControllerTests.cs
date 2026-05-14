using ECommerce.Api.Controllers;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.QueryParameters;
using ECommerce.SharedViewModels.DTOs.Request;
using ECommerce.SharedViewModels.DTOs.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace ECommerce.Tests.Api.Controllers
{
    public class CategoryControllerTests
    {
        private readonly Mock<ILogger<CategoryController>> _logger;
        private readonly Mock<ICategoryService> _categoryService;

        public CategoryControllerTests()
        {
            _logger = new Mock<ILogger<CategoryController>>();
            _categoryService = new Mock<ICategoryService>();
        }

        [Fact]
        public async Task GetAll_ReturnsOk_WithCategories()
        {
            // Arrange
            var categories = new List<CategoryResponse> { new CategoryResponse { Id = 1, Name = "Niche" } };
            _categoryService.Setup(service => service.GetAllAsync()).ReturnsAsync(categories);
            var controller = new CategoryController(_logger.Object, _categoryService.Object);

            // Act
            var result = await controller.GetAll();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var value = Assert.IsType<List<CategoryResponse>>(ok.Value);
            Assert.Single(value);
        }

        [Fact]
        public async Task Get_PassesSameParametersInstance_ToService()
        {
            // Arrange
            CategoryQueryParams? captured = null;
            var parameters = new CategoryQueryParams { SearchTerm = "per" };
            _categoryService
                .Setup(service => service.GetCategoriesAsync(It.IsAny<CategoryQueryParams>()))
                .Callback<CategoryQueryParams>(p => captured = p)
                .ReturnsAsync(new PagedResult<CategoryResponse>(new List<CategoryResponse>(), 0, 1, 10));
            var controller = new CategoryController(_logger.Object, _categoryService.Object);

            // Act
            await controller.Get(parameters);

            // Assert
            Assert.Same(parameters, captured);
        }

        [Fact]
        public async Task GetById_ReturnsOk_WithCategory()
        {
            // Arrange
            var category = new CategoryResponse { Id = 2, Name = "Designer" };
            _categoryService.Setup(service => service.GetCategoryByIdAsync(2)).ReturnsAsync(category);
            var controller = new CategoryController(_logger.Object, _categoryService.Object);

            // Act
            var result = await controller.GetById(2);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var value = Assert.IsType<CategoryResponse>(ok.Value);
            Assert.Equal(2, value.Id);
        }

        [Fact]
        public async Task Create_ReturnsCreated_AndCallsService()
        {
            // Arrange
            var request = new CreateCategoryRequest { Name = "Fresh" };
            _categoryService.Setup(service => service.CreateProductAsync(request)).Returns(Task.CompletedTask);
            var controller = new CategoryController(_logger.Object, _categoryService.Object);

            // Act
            var result = await controller.Create(request);

            // Assert
            Assert.IsType<CreatedResult>(result);
            _categoryService.Verify(service => service.CreateProductAsync(request), Times.Once);
        }

        [Fact]
        public async Task Update_ReturnsNoContent_AndCallsService()
        {
            // Arrange
            var request = new UpdateCategoryRequest { Name = "Woody" };
            _categoryService.Setup(service => service.UpdateCategoryByIdAsync(3, request)).Returns(Task.CompletedTask);
            var controller = new CategoryController(_logger.Object, _categoryService.Object);

            // Act
            var result = await controller.Update(request, 3);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _categoryService.Verify(service => service.UpdateCategoryByIdAsync(3, request), Times.Once);
        }

        [Fact]
        public async Task Delete_ReturnsNoContent_AndCallsService()
        {
            // Arrange
            _categoryService.Setup(service => service.DeleteCategoryByIdAsync(4)).Returns(Task.CompletedTask);
            var controller = new CategoryController(_logger.Object, _categoryService.Object);

            // Act
            var result = await controller.Delete(4);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _categoryService.Verify(service => service.DeleteCategoryByIdAsync(4), Times.Once);
        }
    }
}
