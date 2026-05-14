using ECommerce.Api.Controllers;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.QueryParameters;
using ECommerce.SharedViewModels.DTOs.Response;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ECommerce.Tests.Api.Controllers
{
    public class CustomerControllerTests
    {
        private readonly Mock<ICustomerService> _customerService;

        public CustomerControllerTests()
        {
            _customerService = new Mock<ICustomerService>();
        }

        [Fact]
        public async Task Get_ReturnsOk_WithEmptyPage()
        {
            // Arrange
            var parameters = new CustomerQueryParams();
            _customerService
                .Setup(service => service.GetCustomersAsync(parameters))
                .ReturnsAsync(new PagedResult<CustomerResponse>(new List<CustomerResponse>(), 0, 1, 10));
            var controller = new CustomerController(_customerService.Object);

            // Act
            var result = await controller.Get(parameters);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var value = Assert.IsType<PagedResult<CustomerResponse>>(ok.Value);
            Assert.Empty(value.Items);
            Assert.Equal(0, value.TotalCount);
        }

        [Fact]
        public async Task Get_ReturnsOk_WithPagedCustomers()
        {
            // Arrange
            var parameters = new CustomerQueryParams { PageNumber = 2, PageSize = 5, SearchTerm = "An" };
            var id = Guid.NewGuid();
            var items = new List<CustomerResponse>
            {
                new CustomerResponse
                {
                    Id = id,
                    FullName = "An Tran",
                    Address = "HN",
                    AvatarUrl = null,
                    Email = "an@x.com",
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = null,
                },
            };
            var paged = new PagedResult<CustomerResponse>(items, count: 6, pageNumber: 2, pageSize: 5);
            _customerService.Setup(service => service.GetCustomersAsync(It.IsAny<CustomerQueryParams>())).ReturnsAsync(paged);
            var controller = new CustomerController(_customerService.Object);

            // Act
            var result = await controller.Get(parameters);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var value = Assert.IsType<PagedResult<CustomerResponse>>(ok.Value);
            Assert.Same(items, value.Items);
            Assert.Equal(6, value.TotalCount);
            Assert.Equal(2, value.PageNumber);
            Assert.Equal(5, value.PageSize);
        }

        [Fact]
        public async Task Get_PassesSameParametersInstance_ToService()
        {
            // Arrange
            CustomerQueryParams? captured = null;
            var parameters = new CustomerQueryParams
            {
                SearchTerm = "test",
                PageNumber = 3,
                PageSize = 20,
                SortBy = "FullName",
                IsDescending = true,
            };
            _customerService
                .Setup(service => service.GetCustomersAsync(It.IsAny<CustomerQueryParams>()))
                .Callback<CustomerQueryParams>(p => captured = p)
                .ReturnsAsync(new PagedResult<CustomerResponse>(new List<CustomerResponse>(), 0, 3, 20));
            var controller = new CustomerController(_customerService.Object);

            // Act
            await controller.Get(parameters);

            // Assert
            Assert.Same(parameters, captured);
        }

        [Fact]
        public async Task Get_CallsGetCustomersAsyncOnce()
        {
            // Arrange
            var parameters = new CustomerQueryParams();
            _customerService
                .Setup(service => service.GetCustomersAsync(parameters))
                .ReturnsAsync(new PagedResult<CustomerResponse>(new List<CustomerResponse>(), 0, 1, 10));
            var controller = new CustomerController(_customerService.Object);

            // Act
            await controller.Get(parameters);

            // Assert
            _customerService.Verify(service => service.GetCustomersAsync(parameters), Times.Once);
            _customerService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Get_PropagatesException_WhenServiceThrows()
        {
            // Arrange
            var parameters = new CustomerQueryParams();
            _customerService
                .Setup(service => service.GetCustomersAsync(parameters))
                .ThrowsAsync(new InvalidOperationException("db"));
            var controller = new CustomerController(_customerService.Object);

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => controller.Get(parameters));

            // Assert
            Assert.Equal("db", exception.Message);
        }
    }
}
