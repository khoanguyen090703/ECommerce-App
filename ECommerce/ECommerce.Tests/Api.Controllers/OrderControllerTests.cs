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
    public class OrderControllerTests
    {
        private readonly Mock<ILogger<OrderController>> _logger;
        private readonly Mock<IOrderService> _orderService;

        public OrderControllerTests()
        {
            _logger = new Mock<ILogger<OrderController>>();
            _orderService = new Mock<IOrderService>();
        }

        [Fact]
        public async Task CreateOrder_ReturnsCreated_AndCallsService()
        {
            // Arrange
            var request = new CreateOrderRequest { PaymentMethodId = 1 };
            var created = new CreateOrderResponse { OrderId = 10, RequiresOnlinePayment = false };
            _orderService.Setup(service => service.CreateOrderAsync(request)).ReturnsAsync(created);
            var controller = new OrderController(_logger.Object, _orderService.Object);

            // Act
            var result = await controller.CreateOrder(request);

            // Assert
            var createdResult = Assert.IsType<CreatedResult>(result);
            var body = Assert.IsType<CreateOrderResponse>(createdResult.Value);
            Assert.Equal(10, body.OrderId);
            Assert.False(body.RequiresOnlinePayment);
            _orderService.Verify(service => service.CreateOrderAsync(request), Times.Once);
        }

        [Fact]
        public async Task GetMyOrders_ReturnsOk_WithPagedOrders()
        {
            // Arrange
            var parameters = new OrderQueryParams { PageNumber = 1, PageSize = 10 };
            var paged = new PagedResult<MyOrderResponse>(new List<MyOrderResponse>(), 0, 1, 10);
            _orderService.Setup(service => service.GetMyOrdersAsync(parameters)).ReturnsAsync(paged);
            var controller = new OrderController(_logger.Object, _orderService.Object);

            // Act
            var result = await controller.GetMyOrders(parameters);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.IsType<PagedResult<MyOrderResponse>>(ok.Value);
        }

        [Fact]
        public async Task GetOrders_ReturnsOk_WithPagedOrders()
        {
            // Arrange
            var parameters = new OrderQueryParams { PageNumber = 2, PageSize = 5 };
            var paged = new PagedResult<OrderResponse>(new List<OrderResponse>(), 0, 2, 5);
            _orderService.Setup(service => service.GetOrdersAsync(parameters)).ReturnsAsync(paged);
            var controller = new OrderController(_logger.Object, _orderService.Object);

            // Act
            var result = await controller.GetOrders(parameters);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.IsType<PagedResult<OrderResponse>>(ok.Value);
        }

        [Fact]
        public async Task GetOrderDetails_ReturnsOk_WithDetails()
        {
            // Arrange
            var details = new OrderDetailsResponse { Id = 20 };
            _orderService.Setup(service => service.GetOrderDetailsAsync(20)).ReturnsAsync(details);
            var controller = new OrderController(_logger.Object, _orderService.Object);

            // Act
            var result = await controller.GetOrderDetails(20);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var value = Assert.IsType<OrderDetailsResponse>(ok.Value);
            Assert.Equal(20, value.Id);
        }

        [Fact]
        public async Task GetCheckoutInfo_ReturnsOk_WithInfo()
        {
            // Arrange
            var info = new CheckoutInfoResponse
            {
                Email = "x@y.com",
                CartItems = new List<Item4CartResponse>(),
                PaymentMethods = new List<PaymentMethodResponse>()
            };
            _orderService.Setup(service => service.GetCheckoutInfoAsync()).ReturnsAsync(info);
            var controller = new OrderController(_logger.Object, _orderService.Object);

            // Act
            var result = await controller.GetCheckoutInfo();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.IsType<CheckoutInfoResponse>(ok.Value);
        }

        [Fact]
        public async Task CancelMyOrder_ReturnsOk_WithDetails()
        {
            var details = new OrderDetailsResponse { Id = 30, Status = "Cancelled" };
            _orderService.Setup(service => service.CancelMyOrderAsync(30, It.IsAny<CancellationToken>())).ReturnsAsync(details);
            var controller = new OrderController(_logger.Object, _orderService.Object);

            var result = await controller.CancelMyOrder(30, CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result);
            var value = Assert.IsType<OrderDetailsResponse>(ok.Value);
            Assert.Equal("Cancelled", value.Status);
        }

        [Fact]
        public async Task UpdateOrderStatus_ReturnsOk_WithDetails()
        {
            var request = new UpdateOrderStatusRequest { Status = "Processing" };
            var details = new OrderDetailsResponse { Id = 31, Status = "Processing" };
            _orderService.Setup(service => service.UpdateOrderStatusAsync(31, request, It.IsAny<CancellationToken>())).ReturnsAsync(details);
            var controller = new OrderController(_logger.Object, _orderService.Object);

            var result = await controller.UpdateOrderStatus(31, request, CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result);
            var value = Assert.IsType<OrderDetailsResponse>(ok.Value);
            Assert.Equal("Processing", value.Status);
        }
    }
}
