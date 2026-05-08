using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Interfaces;
using ECommerce.Domain.QueryParameters;
using ECommerce.SharedViewModels.DTOs.Request;
using System;
using System.Collections.Generic;
using System.Text;
using ECommerce.SharedViewModels.DTOs.Response;
using ECommerce.Application.Mappings;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;

        private readonly IPaymentMethodRepository _paymentMethodRepository;

        private readonly IProductVariantRepository _productVariantRepository;

        private readonly ICurrentUserService _currentUserService;

        private readonly ICustomerRepository _customerRepository;

        private readonly IPaymentRepository _paymentRepository;

        private readonly ICartRepository _cartRepository;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserService _userService; // Added userService field

        private readonly ILogger<OrderService> _logger;

        public OrderService(IOrderRepository orderRepository, IPaymentMethodRepository paymentMethodRepository, IProductVariantRepository productVariantRepository, ICurrentUserService currentUserService, ICustomerRepository customerRepository, IPaymentRepository paymentRepository, ICartRepository cartRepository, IUnitOfWork unitOfWork, IUserService userService, ILogger<OrderService> logger)
        {
            _orderRepository = orderRepository;
            _paymentMethodRepository = paymentMethodRepository;
            _productVariantRepository = productVariantRepository;
            _currentUserService = currentUserService;
            _customerRepository = customerRepository;
            _paymentRepository = paymentRepository;
            _cartRepository = cartRepository;
            _unitOfWork = unitOfWork;
            _userService = userService; // Assigning userService to the field
            _logger = logger;
        }

        public async Task<CheckoutInfoResponse> GetCheckoutInfoAsync()
        {
            var userId = _currentUserService.UserId;
            if (userId == null)
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            var customer = await _customerRepository.GetByIdentityIdAsync((Guid)userId);
            if (customer == null)
            {
                throw new UnauthorizedAccessException("User is not allowed to do this feature.");
            }

            var cart = await _cartRepository.GetByCustomerIdAsync(customer.Id.ToString());
            if (cart == null)
            {
                cart = new Cart { Customer = customer };
                await _cartRepository.AddAsync(cart);
            }

            var cartResponse = cart.ToResponse();

            var paymentMethods = await _paymentMethodRepository.GetAllAsync();

            var checkout = new CheckoutInfoResponse
            {
                Email = "",
                CartItems = cartResponse.CartItems,
                SubTotal = cartResponse.Total,
                PaymentMethods = paymentMethods.Select(pm => pm.ToResponse()).ToList()
            };

            // Try to resolve current user email via IUserService
            try
            {
                var profile = await _userService.GetMyProfileAsync();
                if (profile != null)
                {
                    checkout.Email = profile.Email;
                }
            }
            catch
            {
                // ignore and return empty email if user service fails
                _logger.LogWarning("Failed to get user profile for checkout info. Email will be empty.");

            }

            return checkout;
        }

        public async Task CreateOrderAsync(CreateOrderRequest request)
        {
            // get current customer
            var userId = _currentUserService.UserId;
            if (userId == null)
            {
                // Handle the case when the user is not authenticated
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            var customer = await _customerRepository.GetByIdentityIdAsync((Guid)userId);
            if (customer == null)
            {
                throw new UnauthorizedAccessException("User is not allowed to do this feature.");
            }

            var cart = await _cartRepository.GetByCustomerIdAsync(customer.Id.ToString());
            if (cart == null)
            {
                throw new ArgumentException("Cart of this user not found.");
            }

            // check payment method exists
            var paymentMethod = await _paymentMethodRepository.GetByIdAsync(request.PaymentMethodId);
            if (paymentMethod == null)
            {
                throw new ArgumentException("Invalid payment method.");
            }

            // check product variant valid
            var requestedItems = request.OrderItems;
            var newOrderItems = new List<OrderItem>();
            var selectedItems = new List<CartItem>();

            var subTotal = 0m;

            foreach (var requestedItem in requestedItems)
            {
                var productVariant = await _productVariantRepository.GetByIdAsync(requestedItem.ProductVariantId);
                if (productVariant == null)
                {
                    throw new ArgumentException("Invalid product variant.");
                }

                // check variant stock quantity and status
                if (productVariant.StockQuantity < requestedItem.Quantity)
                {
                    throw new ConflictException("Variant stock quantity is insufficent.");
                }

                if(productVariant.Status != VariantStatus.Available)
                {
                    throw new ConflictException("Variant is unavailable.");
                }

                // Create order items
                var newOrderItem = new OrderItem
                {
                    ProductVariant = productVariant,
                    Quantity = requestedItem.Quantity,
                    UnitPrice = productVariant.Price,
                    TotalPrice = requestedItem.Quantity * productVariant.Price
                };
                newOrderItems.Add(newOrderItem);

                subTotal += newOrderItem.TotalPrice;

                // Find selected cart items to remove after order created
                var selectedCartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductVariant.Id == requestedItem.ProductVariantId);
                if (selectedCartItem != null)
                {
                    if (requestedItem.Quantity < selectedCartItem.Quantity)
                    {
                        selectedCartItem.Quantity -= requestedItem.Quantity;
                        selectedCartItem.TotalPrice = selectedCartItem.Quantity * selectedCartItem.UnitPrice;
                    }
                    else if (requestedItem.Quantity == selectedCartItem.Quantity)
                    {
                        cart.CartItems.Remove(selectedCartItem);
                        cart.TotalItems = cart.TotalItems - 1;
                    }
                    else
                    {
                        throw new ArgumentException("Requested quantity is greater than quantity in cart.");
                    }
                }
            }

            // Create order
            var newOrder = new Order
            {
                OrderItems = newOrderItems,
                Customer = customer,
                PaymentStatus = OrderPaymentStatus.Unpaid,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                RecipientName = request.RecipientName,
                PhoneNumber = request.PhoneNumber,
                ShippingAddress = request.ShippingAddress,
                SubTotal = subTotal,
                TotalAmount = subTotal
            };

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                await _orderRepository.AddAsync(newOrder);

                // create payment
                var newPayment = new Payment
                {
                    Amount = subTotal,
                    Order = newOrder,
                    PaymentMethod = paymentMethod,
                    Status = PaymentStatus.Pending
                };
                await _paymentRepository.AddAsync(newPayment);

                await _cartRepository.UpdateAsync(cart);

                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new ConflictException("An error occurred while creating the order. Please try again.");
            }
        }

        public async Task<PagedResult<MyOrderResponse>> GetMyOrdersAsync(OrderQueryParams parameters)
        {
            var userId = _currentUserService.UserId;
            if (userId == null)
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            var customer = await _customerRepository.GetByIdentityIdAsync((Guid)userId);
            if (customer == null)
            {
                throw new UnauthorizedAccessException("User is not allowed to do this feature.");
            }

            var paged = await _orderRepository.GetByCustomerIdAsync(customer.Id, parameters);
            var mapped = paged.Items.Select(o => o.ToMyOrderResponse()).ToList();

            return new PagedResult<MyOrderResponse>(mapped, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<OrderResponse>> GetOrdersAsync(OrderQueryParams parameters)
        {
            if (!_currentUserService.IsAuthenticated)
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            if (!_currentUserService.IsInRole("Admin"))
            {
                throw new ForbiddenException("Only admin can access all orders.");
            }

            var paged = await _orderRepository.GetAsync(parameters);
            var mapped = paged.Items.Select(o => o.ToOrderResponse()).ToList();

            return new PagedResult<OrderResponse>(mapped, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<OrderDetailsResponse> GetOrderDetailsAsync(int id)
        {
            var order = await _orderRepository.GetDetailsByIdAsync(id);
            if (order == null)
            {
                throw new NotFoundException($"Order with id {id} not found.");
            }

            if (!_currentUserService.IsAuthenticated)
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            var isAdmin = _currentUserService.IsInRole("Admin");
            if (!isAdmin)
            {
                var isCustomer = _currentUserService.IsInRole("Customer");
                if (!isCustomer)
                {
                    throw new ForbiddenException("You do not have permission to access this order.");
                }

                var currentUserId = _currentUserService.UserId;
                if (!currentUserId.HasValue || order.Customer.IdentityId != currentUserId.Value)
                {
                    throw new ForbiddenException("You can only access your own orders.");
                }
            }

            return order.ToOrderDetailsResponse();
        }
    }
}
