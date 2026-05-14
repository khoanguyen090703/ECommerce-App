using ECommerce.Api.Controllers;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.QueryParameters;
using ECommerce.SharedViewModels.DTOs.Response;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ECommerce.Tests.Api.Controllers
{
    public class BrandControllerTests
    {
        private readonly Mock<IBrandService> _brandService;

        public BrandControllerTests()
        {
            _brandService = new Mock<IBrandService>();
        }

        [Fact]
        public async Task GetAll_ReturnsOk_WithEmptyList_WhenNoBrands()
        {
            // Arrange
            _brandService
                .Setup(service => service.GetAllBrandsAsync())
                .ReturnsAsync(new List<BrandResponse>());
            var controller = new BrandController(_brandService.Object);

            // Act
            var result = await controller.GetAll();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var value = Assert.IsAssignableFrom<IReadOnlyList<BrandResponse>>(ok.Value);
            Assert.Empty(value);
        }

        [Fact]
        public async Task GetAll_ReturnsOk_WithBrands()
        {
            // Arrange
            var brands = new List<BrandResponse>
            {
                new BrandResponse { Id = 1, Name = "A", ImageUrl = "https://x/a.png" },
                new BrandResponse { Id = 2, Name = "B", ImageUrl = null },
            };
            _brandService.Setup(service => service.GetAllBrandsAsync()).ReturnsAsync(brands);
            var controller = new BrandController(_brandService.Object);

            // Act
            var result = await controller.GetAll();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var value = Assert.IsAssignableFrom<List<BrandResponse>>(ok.Value);
            Assert.Equal(2, value.Count);
            Assert.Equal("A", value[0].Name);
            Assert.Equal("B", value[1].Name);
        }

        [Fact]
        public async Task GetAll_CallsServiceOnce()
        {
            // Arrange
            _brandService.Setup(service => service.GetAllBrandsAsync()).ReturnsAsync(new List<BrandResponse>());
            var controller = new BrandController(_brandService.Object);

            // Act
            await controller.GetAll();

            // Assert
            _brandService.Verify(service => service.GetAllBrandsAsync(), Times.Once);
            _brandService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetAll_PropagatesException_WhenServiceThrows()
        {
            // Arrange
            _brandService
                .Setup(service => service.GetAllBrandsAsync())
                .ThrowsAsync(new InvalidOperationException("db"));
            var controller = new BrandController(_brandService.Object);

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => controller.GetAll());

            // Assert
            Assert.Equal("db", exception.Message);
        }

        [Fact]
        public async Task Get_ReturnsOk_WithPagedResult()
        {
            // Arrange
            var parameters = new BrandQueryParams { PageNumber = 2, PageSize = 5, SearchTerm = "nike" };
            var items = new List<BrandResponse> { new BrandResponse { Id = 1, Name = "Nike" } };
            var paged = new PagedResult<BrandResponse>(items, count: 11, pageNumber: 2, pageSize: 5);
            _brandService.Setup(service => service.GetBrandsAsync(It.IsAny<BrandQueryParams>())).ReturnsAsync(paged);
            var controller = new BrandController(_brandService.Object);

            // Act
            var result = await controller.Get(parameters);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var value = Assert.IsType<PagedResult<BrandResponse>>(ok.Value);
            Assert.Same(items, value.Items);
            Assert.Equal(11, value.TotalCount);
            Assert.Equal(2, value.PageNumber);
            Assert.Equal(5, value.PageSize);
        }

        [Fact]
        public async Task Get_PassesSameParametersInstance_ToService()
        {
            // Arrange
            BrandQueryParams? captured = null;
            var parameters = new BrandQueryParams
            {
                SearchTerm = "test",
                PageNumber = 3,
                PageSize = 20,
                SortBy = "name",
                IsDescending = true,
            };
            _brandService
                .Setup(service => service.GetBrandsAsync(It.IsAny<BrandQueryParams>()))
                .Callback<BrandQueryParams>(p => captured = p)
                .ReturnsAsync(new PagedResult<BrandResponse>(new List<BrandResponse>(), 0, 3, 20));
            var controller = new BrandController(_brandService.Object);

            // Act
            await controller.Get(parameters);

            // Assert
            Assert.Same(parameters, captured);
        }

        [Fact]
        public async Task Get_ReturnsOk_EmptyPage()
        {
            // Arrange
            var parameters = new BrandQueryParams();
            _brandService
                .Setup(service => service.GetBrandsAsync(parameters))
                .ReturnsAsync(new PagedResult<BrandResponse>(new List<BrandResponse>(), 0, 1, 10));
            var controller = new BrandController(_brandService.Object);

            // Act
            var result = await controller.Get(parameters);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var value = Assert.IsType<PagedResult<BrandResponse>>(ok.Value);
            Assert.Empty(value.Items);
            Assert.Equal(0, value.TotalCount);
        }

        [Fact]
        public async Task Get_CallsGetBrandsAsyncOnce()
        {
            // Arrange
            var parameters = new BrandQueryParams();
            _brandService
                .Setup(service => service.GetBrandsAsync(parameters))
                .ReturnsAsync(new PagedResult<BrandResponse>(new List<BrandResponse>(), 0, 1, 10));
            var controller = new BrandController(_brandService.Object);

            // Act
            await controller.Get(parameters);

            // Assert
            _brandService.Verify(service => service.GetBrandsAsync(parameters), Times.Once);
            _brandService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Get_PropagatesException_WhenServiceThrows()
        {
            // Arrange
            var parameters = new BrandQueryParams();
            _brandService
                .Setup(service => service.GetBrandsAsync(parameters))
                .ThrowsAsync(new InvalidOperationException("db"));
            var controller = new BrandController(_brandService.Object);

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => controller.Get(parameters));

            // Assert
            Assert.Equal("db", exception.Message);
        }
    }
}
