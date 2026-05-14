using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Interfaces;
using ECommerce.Domain.QueryParameters;
using ECommerce.SharedViewModels.DTOs.Request;
using ECommerce.SharedViewModels.DTOs.Response;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Net.Http;

namespace ECommerce.Tests.Application.Services;

public class OrderServiceTests
{
    [Fact]
    public async Task GetCheckoutInfoAsync_WhenUserNotAuthenticated_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns((Guid?)null);
        var sut = CreateSut(currentUser);

        // Act
        var act = async () => await sut.GetCheckoutInfoAsync();

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CreateOrderAsync_WhenCartMissing_ThrowsArgumentException()
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
        var sut = CreateSut(currentUser, customerRepo, cartRepo);

        // Act
        var act = async () => await sut.CreateOrderAsync(new CreateOrderRequest
        {
            RecipientName = "A",
            PhoneNumber = "1",
            ShippingAddress = "Addr",
            PaymentMethodId = 1,
            OrderItems = new List<Item4CreateOrderRequest>()
        });

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Cart*");
    }

    [Fact]
    public async Task GetOrdersAsync_WhenNotAdmin_ThrowsForbiddenException()
    {
        // Arrange
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.IsAuthenticated).Returns(true);
        currentUser.Setup(c => c.IsInRole("Admin")).Returns(false);
        var sut = CreateSut(currentUser);

        // Act
        var act = async () => await sut.GetOrdersAsync(new OrderQueryParams());

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetOrderDetailsAsync_WhenOrderMissing_ThrowsNotFoundException()
    {
        // Arrange
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.IsAuthenticated).Returns(true);
        currentUser.Setup(c => c.IsInRole("Admin")).Returns(true);
        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(r => r.GetDetailsByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);
        var sut = CreateSut(currentUser, orderRepo: orderRepo);

        // Act
        var act = async () => await sut.GetOrderDetailsAsync(5);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetCheckoutInfoAsync_WhenUserServiceThrows_ReturnsCheckoutWithEmptyEmail()
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
        var paymentMethodRepo = new Mock<IPaymentMethodRepository>();
        paymentMethodRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<PaymentMethod>());
        var userService = new Mock<IUserService>();
        userService.Setup(u => u.GetMyProfileAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new HttpRequestException("unavailable"));
        var sut = CreateSut(
            currentUser,
            customerRepo,
            cartRepo,
            paymentMethodRepo: paymentMethodRepo,
            userService: userService);

        // Act
        var checkout = await sut.GetCheckoutInfoAsync();

        // Assert
        checkout.Email.Should().BeEmpty();
        checkout.PaymentMethods.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateOrderAsync_WhenValid_CommitsTransactionAndPersists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var customer = new Customer { Id = Guid.NewGuid(), IdentityId = userId, FullName = "C" };
        var product = new Product { Id = 10, Status = ProductStatus.Active, Name = "P" };
        var variant = new ProductVariant
        {
            Id = 20,
            Price = 100m,
            StockQuantity = 5,
            Status = VariantStatus.Available,
            Product = product,
            Name = "V"
        };
        product.ProductVariants = new List<ProductVariant> { variant };

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(userId);
        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo.Setup(r => r.GetByIdentityIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        customerRepo.Setup(r => r.GetByIdentityIdAsync(userId)).ReturnsAsync(customer);
        var cartItem = new CartItem
        {
            Id = 1,
            Quantity = 2,
            UnitPrice = 100m,
            TotalPrice = 200m,
            ProductVariant = variant
        };
        var cart = new Cart
        {
            Id = 1,
            Customer = customer,
            TotalItems = 1,
            CartItems = new List<CartItem> { cartItem }
        };
        cartItem.Cart = cart;

        var cartRepo = new Mock<ICartRepository>();
        cartRepo.Setup(r => r.GetByCustomerIdAsync(customer.Id.ToString())).ReturnsAsync(cart);
        var paymentMethodRepo = new Mock<IPaymentMethodRepository>();
        paymentMethodRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new PaymentMethod { Id = 1, Name = "COD" });
        var variantRepo = new Mock<IProductVariantRepository>();
        variantRepo.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(variant);
        var productRepo = new Mock<IProductRepository>();
        productRepo.Setup(r => r.GetByIdAsync(10, true)).ReturnsAsync(product);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(r => r.AddAsync(It.IsAny<Order>()))
            .Callback<Order>(o => { o.Id = 77; })
            .Returns(Task.CompletedTask);
        var paymentRepo = new Mock<IPaymentRepository>();
        var userService = new Mock<IUserService>();
        userService.Setup(u => u.GetMyProfileAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfileResponse { Id = "x", FullName = "N", Email = "e@e.com" });

        var sut = CreateSut(
            currentUser,
            customerRepo,
            cartRepo,
            orderRepo,
            paymentMethodRepo,
            variantRepo,
            productRepo,
            paymentRepo,
            unitOfWork,
            userService);

        var request = new CreateOrderRequest
        {
            RecipientName = "R",
            PhoneNumber = "090",
            ShippingAddress = "Addr",
            PaymentMethodId = 1,
            OrderItems = new List<Item4CreateOrderRequest>
            {
                new() { ProductVariantId = 20, Quantity = 1 }
            }
        };

        // Act
        var created = await sut.CreateOrderAsync(request);

        // Assert
        created.OrderId.Should().Be(77);
        created.RequiresOnlinePayment.Should().BeFalse();
        created.PaymentStatus.Should().Be("Unpaid");
        created.CanRetryOnlinePayment.Should().BeFalse();
        unitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Once);
        unitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
        unitOfWork.Verify(u => u.RollbackTransactionAsync(), Times.Never);
        orderRepo.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Once);
        paymentRepo.Verify(r => r.AddAsync(It.IsAny<Payment>()), Times.Once);
        cartRepo.Verify(r => r.UpdateAsync(cart), Times.Once);
        variant.StockQuantity.Should().Be(4);
    }

    [Fact]
    public async Task CreateOrderAsync_WhenPersistThrows_RollsBackAndThrowsConflictException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var customer = new Customer { Id = Guid.NewGuid(), IdentityId = userId, FullName = "C" };
        var product = new Product { Id = 10, Status = ProductStatus.Active, Name = "P" };
        var variant = new ProductVariant
        {
            Id = 20,
            Price = 10m,
            StockQuantity = 3,
            Status = VariantStatus.Available,
            Product = product,
            Name = "V"
        };
        product.ProductVariants = new List<ProductVariant> { variant };

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(userId);
        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo.Setup(r => r.GetByIdentityIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        customerRepo.Setup(r => r.GetByIdentityIdAsync(userId)).ReturnsAsync(customer);
        var cartItem = new CartItem
        {
            Id = 1,
            Quantity = 1,
            UnitPrice = 10m,
            TotalPrice = 10m,
            ProductVariant = variant
        };
        var cart = new Cart { Id = 1, Customer = customer, TotalItems = 1, CartItems = new List<CartItem> { cartItem } };
        cartItem.Cart = cart;

        var cartRepo = new Mock<ICartRepository>();
        cartRepo.Setup(r => r.GetByCustomerIdAsync(customer.Id.ToString())).ReturnsAsync(cart);
        var paymentMethodRepo = new Mock<IPaymentMethodRepository>();
        paymentMethodRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new PaymentMethod { Id = 1, Name = "COD" });
        var variantRepo = new Mock<IProductVariantRepository>();
        variantRepo.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(variant);
        var productRepo = new Mock<IProductRepository>();
        productRepo.Setup(r => r.GetByIdAsync(10, true)).ReturnsAsync(product);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.CommitTransactionAsync()).ThrowsAsync(new InvalidOperationException("db"));
        unitOfWork.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);
        var orderRepo = new Mock<IOrderRepository>();
        var paymentRepo = new Mock<IPaymentRepository>();
        var userService = new Mock<IUserService>();

        var sut = CreateSut(
            currentUser,
            customerRepo,
            cartRepo,
            orderRepo,
            paymentMethodRepo,
            variantRepo,
            productRepo,
            paymentRepo,
            unitOfWork,
            userService);

        var request = new CreateOrderRequest
        {
            RecipientName = "R",
            PhoneNumber = "1",
            ShippingAddress = "A",
            PaymentMethodId = 1,
            OrderItems = new List<Item4CreateOrderRequest> { new() { ProductVariantId = 20, Quantity = 1 } }
        };

        // Act
        var act = async () => await sut.CreateOrderAsync(request);

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
        unitOfWork.Verify(u => u.RollbackTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task CancelMyOrderAsync_WhenOrderNotPending_ThrowsConflictException()
    {
        var userId = Guid.NewGuid();
        var customer = new Customer { Id = Guid.NewGuid(), IdentityId = userId, FullName = "C" };
        var order = new Order
        {
            Id = 9,
            Status = OrderStatus.Processing,
            Customer = customer,
            OrderItems = new List<OrderItem>()
        };

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(userId);
        currentUser.Setup(c => c.IsInRole("Customer")).Returns(true);

        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(r => r.GetByIdForUpdateAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var sut = CreateSut(currentUser, orderRepo: orderRepo);

        var act = async () => await sut.CancelMyOrderAsync(9);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_WhenTransitionInvalid_ThrowsConflictException()
    {
        var order = new Order
        {
            Id = 11,
            Status = OrderStatus.Pending,
            Customer = new Customer { IdentityId = Guid.NewGuid() },
            OrderItems = new List<OrderItem>()
        };

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.IsAuthenticated).Returns(true);
        currentUser.Setup(c => c.IsInRole("Admin")).Returns(true);

        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(r => r.GetByIdForUpdateAsync(11, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var sut = CreateSut(currentUser, orderRepo: orderRepo);

        var act = async () => await sut.UpdateOrderStatusAsync(11, new UpdateOrderStatusRequest { Status = "Delivered" });

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_WhenAdminCancelsFromProcessing_ThrowsConflictException()
    {
        var order = new Order
        {
            Id = 12,
            Status = OrderStatus.Processing,
            Customer = new Customer { IdentityId = Guid.NewGuid() },
            OrderItems = new List<OrderItem>()
        };

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.IsAuthenticated).Returns(true);
        currentUser.Setup(c => c.IsInRole("Admin")).Returns(true);

        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(r => r.GetByIdForUpdateAsync(12, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var sut = CreateSut(currentUser, orderRepo: orderRepo);

        var act = async () => await sut.UpdateOrderStatusAsync(12, new UpdateOrderStatusRequest { Status = "Cancelled" });

        await act.Should().ThrowAsync<ConflictException>();
    }

    private static OrderService CreateSut(
        Mock<ICurrentUserService> currentUser,
        Mock<ICustomerRepository>? customerRepo = null,
        Mock<ICartRepository>? cartRepo = null,
        Mock<IOrderRepository>? orderRepo = null,
        Mock<IPaymentMethodRepository>? paymentMethodRepo = null,
        Mock<IProductVariantRepository>? variantRepo = null,
        Mock<IProductRepository>? productRepo = null,
        Mock<IPaymentRepository>? paymentRepo = null,
        Mock<IUnitOfWork>? unitOfWork = null,
        Mock<IUserService>? userService = null)
    {
        customerRepo ??= new Mock<ICustomerRepository>();
        cartRepo ??= new Mock<ICartRepository>();
        orderRepo ??= new Mock<IOrderRepository>();
        paymentMethodRepo ??= new Mock<IPaymentMethodRepository>();
        variantRepo ??= new Mock<IProductVariantRepository>();
        productRepo ??= new Mock<IProductRepository>();
        paymentRepo ??= new Mock<IPaymentRepository>();
        unitOfWork ??= new Mock<IUnitOfWork>();
        userService ??= new Mock<IUserService>();

        return new OrderService(
            orderRepo.Object,
            paymentMethodRepo.Object,
            variantRepo.Object,
            productRepo.Object,
            currentUser.Object,
            customerRepo.Object,
            paymentRepo.Object,
            cartRepo.Object,
            unitOfWork.Object,
            userService.Object,
            NullLogger<OrderService>.Instance);
    }
}
