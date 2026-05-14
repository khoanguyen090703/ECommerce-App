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

        private readonly IProductRepository _productRepository;

        private readonly ICurrentUserService _currentUserService;

        private readonly ICustomerRepository _customerRepository;

        private readonly IPaymentRepository _paymentRepository;

        private readonly ICartRepository _cartRepository;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserService _userService; // Added userService field

        private readonly ILogger<OrderService> _logger;

        public OrderService(IOrderRepository orderRepository, IPaymentMethodRepository paymentMethodRepository, IProductVariantRepository productVariantRepository, IProductRepository productRepository, ICurrentUserService currentUserService, ICustomerRepository customerRepository, IPaymentRepository paymentRepository, ICartRepository cartRepository, IUnitOfWork unitOfWork, IUserService userService, ILogger<OrderService> logger)
        {
            _orderRepository = orderRepository;
            _paymentMethodRepository = paymentMethodRepository;
            _productVariantRepository = productVariantRepository;
            _productRepository = productRepository;
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

        public async Task<CreateOrderResponse> CreateOrderAsync(CreateOrderRequest request)
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
            var variantCache = new Dictionary<int, ProductVariant>();

            foreach (var requestedItem in requestedItems)
            {
                if (!variantCache.TryGetValue(requestedItem.ProductVariantId, out var productVariant))
                {
                    productVariant = await _productVariantRepository.GetByIdAsync(requestedItem.ProductVariantId);
                    if (productVariant == null)
                    {
                        throw new ArgumentException("Invalid product variant.");
                    }

                    variantCache[requestedItem.ProductVariantId] = productVariant;
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

                productVariant.StockQuantity -= requestedItem.Quantity;
                if (productVariant.Status != VariantStatus.Discontinued && productVariant.StockQuantity <= 0)
                {
                    productVariant.Status = VariantStatus.OutOfStock;
                }

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

                var affectedProductIds = variantCache.Values
                    .Select(v => v.Product?.Id ?? 0)
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

                foreach (var productId in affectedProductIds)
                {
                    var product = await _productRepository.GetByIdAsync(productId, includeProductVariants: true);
                    if (product == null || product.Status != ProductStatus.Active)
                        continue;

                    var anyAvailable = product.ProductVariants.Any(v => v.Status == VariantStatus.Available);
                    if (!anyAvailable)
                        product.Status = ProductStatus.Inactive;
                }

                await _unitOfWork.CommitTransactionAsync();

                var requiresOnline = IsOnlineCheckoutPaymentMethod(paymentMethod.Name);
                return new CreateOrderResponse
                {
                    OrderId = newOrder.Id,
                    RequiresOnlinePayment = requiresOnline,
                    PaymentStatus = newOrder.PaymentStatus.ToString(),
                    CanRetryOnlinePayment = requiresOnline,
                };
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new ConflictException("An error occurred while creating the order. Please try again.");
            }
        }

        /// <summary>
        /// Stripe Checkout applies to methods named Stripe or legacy VnPay until migrated.
        /// </summary>
        private static bool IsOnlineCheckoutPaymentMethod(string paymentMethodName) =>
            OrderMappings.IsOnlineCheckoutPaymentMethodName(paymentMethodName);

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

        public async Task<OrderDetailsResponse> CancelMyOrderAsync(int id, CancellationToken cancellationToken = default)
        {
            var userId = RequireCustomerUserId();

            var order = await _orderRepository.GetByIdForUpdateAsync(id, cancellationToken);
            if (order == null)
                throw new NotFoundException($"Order with id {id} not found.");

            if (order.Customer.IdentityId != userId)
                throw new ForbiddenException("You can only cancel your own orders.");

            if (order.Status != OrderStatus.Pending)
                throw new ConflictException("Only pending orders can be cancelled.");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                ApplyOrderCancellation(order);
                await _orderRepository.UpdateAsync(order);
                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception ex) when (ex is not ConflictException and not ForbiddenException and not NotFoundException and not UnauthorizedAccessException)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new ConflictException("An error occurred while cancelling the order. Please try again.");
            }

            return await GetOrderDetailsAsync(id);
        }

        public async Task<OrderDetailsResponse> UpdateOrderStatusAsync(
            int id,
            UpdateOrderStatusRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!_currentUserService.IsAuthenticated)
                throw new UnauthorizedAccessException("User is not authenticated.");

            if (!_currentUserService.IsInRole("Admin"))
                throw new ForbiddenException("Only admin can update order status.");

            if (string.IsNullOrWhiteSpace(request.Status))
                throw new ArgumentException("Order status is required.");

            if (!Enum.TryParse<OrderStatus>(request.Status.Trim(), true, out var targetStatus))
                throw new ArgumentException("Invalid order status.");

            var order = await _orderRepository.GetByIdForUpdateAsync(id, cancellationToken);
            if (order == null)
                throw new NotFoundException($"Order with id {id} not found.");

            if (!IsAllowedAdminStatusTransition(order.Status, targetStatus))
                throw new ConflictException($"Cannot change order status from {order.Status} to {targetStatus}.");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                if (targetStatus == OrderStatus.Cancelled)
                {
                    ApplyOrderCancellation(order);
                }
                else
                {
                    order.Status = targetStatus;
                }

                if (targetStatus == OrderStatus.Delivered)
                    order.CompletedDate = DateTime.UtcNow;

                await _orderRepository.UpdateAsync(order);
                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception ex) when (ex is not ConflictException and not ForbiddenException and not NotFoundException and not UnauthorizedAccessException and not ArgumentException)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new ConflictException("An error occurred while updating the order status. Please try again.");
            }

            return await GetOrderDetailsAsync(id);
        }

        private Guid RequireCustomerUserId()
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            if (!_currentUserService.IsInRole("Customer"))
                throw new ForbiddenException("Only customers can perform this action.");

            return userId.Value;
        }

        private static bool IsAllowedAdminStatusTransition(OrderStatus current, OrderStatus target)
        {
            if (current == target)
                return false;

            return (current, target) switch
            {
                (OrderStatus.Pending, OrderStatus.Processing) => true,
                (OrderStatus.Pending, OrderStatus.Cancelled) => true,
                (OrderStatus.Processing, OrderStatus.Shipping) => true,
                (OrderStatus.Shipping, OrderStatus.Delivered) => true,
                _ => false
            };
        }

        private static void ApplyOrderCancellation(Order order)
        {
            order.Status = OrderStatus.Cancelled;
            order.CancelledDate = DateTime.UtcNow;
            RestoreOrderItemsStock(order.OrderItems);
        }

        private static void RestoreOrderItemsStock(IEnumerable<OrderItem> orderItems)
        {
            foreach (var item in orderItems)
            {
                var variant = item.ProductVariant;
                variant.StockQuantity += item.Quantity;

                if (variant.Status == VariantStatus.OutOfStock && variant.StockQuantity > 0)
                    variant.Status = VariantStatus.Available;
            }
        }
    }
}
