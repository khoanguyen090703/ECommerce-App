using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Interfaces;
using ECommerce.SharedViewModels.DTOs.Request;
using FluentAssertions;
using Moq;

namespace ECommerce.Tests.Application.Services;

public class CartServiceTests
{
    [Fact]
    public async Task GetCartItemCountForCurrentCustomerAsync_WhenNoCart_ReturnsZero()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var customer = new Customer { Id = Guid.NewGuid(), IdentityId = userId, FullName = "C" };
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(userId);
        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo.Setup(r => r.GetByIdentityIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        customerRepo.Setup(r => r.GetByIdentityIdAsync(userId)).ReturnsAsync(customer);
        var cartRepo = new Mock<ICartRepository>();
        cartRepo.Setup(r => r.GetByCustomerIdAsync(customer.Id.ToString())).ReturnsAsync((Cart?)null);
        var variantRepo = new Mock<IProductVariantRepository>();
        var cartItemRepo = new Mock<ICartItemRepository>();
        var sut = new CartService(cartRepo.Object, currentUser.Object, customerRepo.Object, variantRepo.Object, cartItemRepo.Object);

        // Act
        var count = await sut.GetCartItemCountForCurrentCustomerAsync();

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public async Task AddItemToCartAsync_WhenVariantNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var customer = new Customer { Id = Guid.NewGuid(), IdentityId = userId, FullName = "C" };
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(userId);
        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo.Setup(r => r.GetByIdentityIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        customerRepo.Setup(r => r.GetByIdentityIdAsync(userId)).ReturnsAsync(customer);
        var cart = new Cart { Id = 1, Customer = customer, TotalItems = 0, CartItems = new List<CartItem>() };
        var cartRepo = new Mock<ICartRepository>();
        cartRepo.Setup(r => r.GetByCustomerIdAsync(customer.Id.ToString())).ReturnsAsync(cart);
        var variantRepo = new Mock<IProductVariantRepository>();
        variantRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((ProductVariant?)null);
        var cartItemRepo = new Mock<ICartItemRepository>();
        var sut = new CartService(cartRepo.Object, currentUser.Object, customerRepo.Object, variantRepo.Object, cartItemRepo.Object);

        // Act
        var act = async () => await sut.AddItemToCartAsync(new AddCartItemRequest { ProductVariantId = 999 });

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ClearCartAsync_WhenCartMissing_ThrowsNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var customer = new Customer { Id = Guid.NewGuid(), IdentityId = userId, FullName = "C" };
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(userId);
        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo.Setup(r => r.GetByIdentityIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        customerRepo.Setup(r => r.GetByIdentityIdAsync(userId)).ReturnsAsync(customer);
        var cartRepo = new Mock<ICartRepository>();
        cartRepo.Setup(r => r.GetByCustomerIdAsync(customer.Id.ToString())).ReturnsAsync((Cart?)null);
        var variantRepo = new Mock<IProductVariantRepository>();
        var cartItemRepo = new Mock<ICartItemRepository>();
        var sut = new CartService(cartRepo.Object, currentUser.Object, customerRepo.Object, variantRepo.Object, cartItemRepo.Object);

        // Act
        var act = async () => await sut.ClearCartAsync();

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetCartByCurrentCustomerOrCreateCartAsync_WhenCartMissing_CreatesCartAndReturnsResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var customer = new Customer { Id = Guid.NewGuid(), IdentityId = userId, FullName = "C" };
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(userId);
        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo.Setup(r => r.GetByIdentityIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        customerRepo.Setup(r => r.GetByIdentityIdAsync(userId)).ReturnsAsync(customer);
        var cartRepo = new Mock<ICartRepository>();
        cartRepo.Setup(r => r.GetByCustomerIdAsync(customer.Id.ToString())).ReturnsAsync((Cart?)null);
        var variantRepo = new Mock<IProductVariantRepository>();
        var cartItemRepo = new Mock<ICartItemRepository>();
        var sut = new CartService(cartRepo.Object, currentUser.Object, customerRepo.Object, variantRepo.Object, cartItemRepo.Object);

        // Act
        var result = await sut.GetCartByCurrentCustomerOrCreateCartAsync();

        // Assert
        result.Should().NotBeNull();
        cartRepo.Verify(r => r.AddAsync(It.Is<Cart>(c => c.Customer == customer)), Times.Once);
    }

    [Fact]
    public async Task GetCartItemCountForCurrentCustomerAsync_WhenUserNotAuthenticated_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns((Guid?)null);
        var sut = new CartService(
            new Mock<ICartRepository>().Object,
            currentUser.Object,
            new Mock<ICustomerRepository>().Object,
            new Mock<IProductVariantRepository>().Object,
            new Mock<ICartItemRepository>().Object);

        // Act
        var act = async () => await sut.GetCartItemCountForCurrentCustomerAsync();

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task AddItemToCartAsync_WhenProductInactive_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var customer = new Customer { Id = Guid.NewGuid(), IdentityId = userId, FullName = "C" };
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(userId);
        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo.Setup(r => r.GetByIdentityIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        customerRepo.Setup(r => r.GetByIdentityIdAsync(userId)).ReturnsAsync(customer);
        var product = new Product { Status = ProductStatus.Inactive, Name = "P", Description = "d", Brand = new Brand { Name = "B" }, Line = "L" };
        var variant = new ProductVariant { Id = 5, Product = product, StockQuantity = 5, Status = VariantStatus.Available, Price = 10m, Name = "V" };
        var cart = new Cart { Id = 1, Customer = customer, TotalItems = 0, CartItems = new List<CartItem>() };
        var cartRepo = new Mock<ICartRepository>();
        cartRepo.Setup(r => r.GetByCustomerIdAsync(customer.Id.ToString())).ReturnsAsync(cart);
        var variantRepo = new Mock<IProductVariantRepository>();
        variantRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(variant);
        var cartItemRepo = new Mock<ICartItemRepository>();
        var sut = new CartService(cartRepo.Object, currentUser.Object, customerRepo.Object, variantRepo.Object, cartItemRepo.Object);

        // Act
        var act = async () => await sut.AddItemToCartAsync(new AddCartItemRequest { ProductVariantId = 5 });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not available*");
    }

    [Fact]
    public async Task AddItemToCartAsync_WhenLineAlreadyExists_IncrementsQuantityAndUpdatesCartItem()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var customer = new Customer { Id = Guid.NewGuid(), IdentityId = userId, FullName = "C" };
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(userId);
        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo.Setup(r => r.GetByIdentityIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        customerRepo.Setup(r => r.GetByIdentityIdAsync(userId)).ReturnsAsync(customer);
        var product = new Product { Status = ProductStatus.Active, Name = "P", Description = "d", Brand = new Brand { Name = "B" }, Line = "L" };
        var variant = new ProductVariant { Id = 9, Product = product, StockQuantity = 10, Status = VariantStatus.Available, Price = 5m, Name = "V" };
        var cart = new Cart { Id = 3, Customer = customer, TotalItems = 1, CartItems = new List<CartItem>() };
        var existingItem = new CartItem { Id = 100, Quantity = 2, UnitPrice = 5m, TotalPrice = 10m, ProductVariant = variant, Cart = cart };
        cart.CartItems.Add(existingItem);
        var cartRepo = new Mock<ICartRepository>();
        cartRepo.Setup(r => r.GetByCustomerIdAsync(customer.Id.ToString())).ReturnsAsync(cart);
        var variantRepo = new Mock<IProductVariantRepository>();
        variantRepo.Setup(r => r.GetByIdAsync(9)).ReturnsAsync(variant);
        var cartItemRepo = new Mock<ICartItemRepository>();
        cartItemRepo.Setup(r => r.GetByCartIdAndProductVariantIdAsync(3, 9)).ReturnsAsync(existingItem);
        var sut = new CartService(cartRepo.Object, currentUser.Object, customerRepo.Object, variantRepo.Object, cartItemRepo.Object);

        // Act
        await sut.AddItemToCartAsync(new AddCartItemRequest { ProductVariantId = 9 });

        // Assert
        existingItem.Quantity.Should().Be(3);
        existingItem.TotalPrice.Should().Be(15m);
        cartItemRepo.Verify(r => r.UpdateAsync(existingItem), Times.Once);
        cartRepo.Verify(r => r.UpdateAsync(It.IsAny<Cart>()), Times.Never);
    }
}
