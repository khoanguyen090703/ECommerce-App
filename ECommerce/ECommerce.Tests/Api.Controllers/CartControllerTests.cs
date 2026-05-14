using ECommerce.Api.Controllers;
using ECommerce.Application.Interfaces;
using ECommerce.SharedViewModels.DTOs.Request;
using ECommerce.SharedViewModels.DTOs.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace ECommerce.Tests.Api.Controllers
{
    public class CartControllerTests
    {
        private readonly Mock<ILogger<CartController>> _logger;
        private readonly Mock<ICartService> _cartService;

        public CartControllerTests()
        {
            _logger = new Mock<ILogger<CartController>>();
            _cartService = new Mock<ICartService>();
        }

        [Fact]
        public async Task GetCart_ReturnsOk_WithCart()
        {
            // Arrange
            _cartService.Setup(service => service.GetCartByCurrentCustomerOrCreateCartAsync()).ReturnsAsync(new CartResponse());
            var controller = new CartController(_logger.Object, _cartService.Object);

            // Act
            var result = await controller.GetCart();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.IsType<CartResponse>(ok.Value);
        }

        [Fact]
        public async Task GetItemCount_ReturnsOk_WithTotalItems()
        {
            // Arrange
            _cartService.Setup(service => service.GetCartItemCountForCurrentCustomerAsync()).ReturnsAsync(3);
            var controller = new CartController(_logger.Object, _cartService.Object);

            // Act
            var result = await controller.GetItemCount();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var value = Assert.IsType<CartItemCountResponse>(ok.Value);
            Assert.Equal(3, value.TotalItems);
        }

        [Fact]
        public async Task AddItemToCart_ReturnsCreated_AndCallsService()
        {
            // Arrange
            var request = new AddCartItemRequest { ProductVariantId = 1 };
            _cartService.Setup(service => service.AddItemToCartAsync(request)).Returns(Task.CompletedTask);
            var controller = new CartController(_logger.Object, _cartService.Object);

            // Act
            var result = await controller.AddItemToCart(request);

            // Assert
            Assert.IsType<CreatedResult>(result);
            _cartService.Verify(service => service.AddItemToCartAsync(request), Times.Once);
        }

        [Fact]
        public async Task UpdateCartItem_ReturnsOk_AndCallsService()
        {
            // Arrange
            var request = new UpdateCartItemQuantityRequest { Quantity = 5 };
            _cartService.Setup(service => service.UpdateCartItemQuantityAsync(11, request)).Returns(Task.CompletedTask);
            var controller = new CartController(_logger.Object, _cartService.Object);

            // Act
            var result = await controller.UpdateCartItem(11, request);

            // Assert
            Assert.IsType<OkResult>(result);
            _cartService.Verify(service => service.UpdateCartItemQuantityAsync(11, request), Times.Once);
        }

        [Fact]
        public async Task RemoveItemFromCart_ReturnsOk_AndCallsService()
        {
            // Arrange
            _cartService.Setup(service => service.DeleteCartItemAsync(12)).Returns(Task.CompletedTask);
            var controller = new CartController(_logger.Object, _cartService.Object);

            // Act
            var result = await controller.RemoveItemFromCart(12);

            // Assert
            Assert.IsType<OkResult>(result);
            _cartService.Verify(service => service.DeleteCartItemAsync(12), Times.Once);
        }

        [Fact]
        public async Task ClearCart_ReturnsOk_AndCallsService()
        {
            // Arrange
            _cartService.Setup(service => service.ClearCartAsync()).Returns(Task.CompletedTask);
            var controller = new CartController(_logger.Object, _cartService.Object);

            // Act
            var result = await controller.ClearCart();

            // Assert
            Assert.IsType<OkResult>(result);
            _cartService.Verify(service => service.ClearCartAsync(), Times.Once);
        }
    }
}
