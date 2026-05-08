using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Mappings;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Interfaces;
using ECommerce.SharedViewModels.DTOs.Request;
using ECommerce.SharedViewModels.DTOs.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Application.Services
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepository;

        private readonly ICurrentUserService _currentUserService;

        private readonly ICustomerRepository _customerRepository;

        private readonly IProductVariantRepository _productVariantRepository;

        private readonly ICartItemRepository _cartItemRepository;

        public CartService(ICartRepository cartRepository, ICurrentUserService currentUserService, ICustomerRepository customerRepository, IProductVariantRepository productVariantRepository, ICartItemRepository cartItemRepository)
        {
            _cartRepository = cartRepository;
            _currentUserService = currentUserService;
            _customerRepository = customerRepository;
            _productVariantRepository = productVariantRepository;
            _cartItemRepository = cartItemRepository;
        }

        public async Task<int> GetCartItemCountForCurrentCustomerAsync()
        {
            var customer = await GetCustomerByCurrentUserService();

            var cart = await _cartRepository.GetByCustomerIdAsync(customer.Id.ToString());
            if (cart == null)
            {
                return 0;
            }

            return cart.TotalItems;
        }

        public async Task AddItemToCartAsync(AddCartItemRequest request)
        {
            var customer = await GetCustomerByCurrentUserService();

            var cart = await _cartRepository.GetByCustomerIdAsync(customer.Id.ToString());

            if (cart == null)
            {
                cart = new Cart();
                cart.Customer = customer;
                await _cartRepository.AddAsync(cart);
            }

            var variant = await _productVariantRepository.GetByIdAsync(request.ProductVariantId);
            if (variant == null)
            {
                throw new NotFoundException($"Product variant with ID {request.ProductVariantId} not found.");
            }

            // Check product status
            if (variant.Product.Status != ProductStatus.Active)
            {
                throw new InvalidOperationException("Product is not available for purchase.");
            }

            // Check stock quantity and variant status
            if (variant.StockQuantity < 1 || variant.Status != VariantStatus.Available)
            {
                throw new InvalidOperationException("Product variant is out of stock.");
            }

            // Check existed cart item
            var existingCartItem = await _cartItemRepository.GetByCartIdAndProductVariantIdAsync(cart.Id, variant.Id);
            if (existingCartItem != null)
            {
                var variantRemaining = existingCartItem.ProductVariant.StockQuantity;
                if (existingCartItem.Quantity + 1 > variantRemaining)
                {
                    throw new InvalidOperationException("Stock quantity of this item is insufficient.");
                }

                existingCartItem.Quantity += 1;
                existingCartItem.TotalPrice = existingCartItem.Quantity * existingCartItem.UnitPrice;
                await _cartItemRepository.UpdateAsync(existingCartItem);
                return;
            }

            var newItem = new CartItem
            {
                Cart = cart,
                ProductVariant = variant,
                Quantity = 1,
                UnitPrice = variant.Price,
                TotalPrice = variant.Price
            };
            cart.CartItems.Add(newItem);
            cart.TotalItems = cart.CartItems.Count;

            await _cartRepository.UpdateAsync(cart);
        }

        public async Task ClearCartAsync()
        {
            var customer = await GetCustomerByCurrentUserService();

            var cart = await _cartRepository.GetByCustomerIdAsync(customer.Id.ToString());

            if (cart == null)
            {
                throw new NotFoundException("Cart not found.");
            }

            cart.CartItems.Clear();
            cart.TotalItems = 0;

            await _cartRepository.UpdateAsync(cart);
        }

        public async Task DeleteCartItemAsync(int itemId)
        {
            var customer = await GetCustomerByCurrentUserService();

            var cart = await _cartRepository.GetByCustomerIdAsync(customer.Id.ToString());

            var cartItem = await _cartItemRepository.GetByIdAsync(itemId);
            if (cartItem == null)
            {
                throw new NotFoundException($"Cart item with ID {itemId} not found.");
            }

            if (cartItem.Cart.Id != cart.Id)
            {
                throw new UnauthorizedAccessException("User is not allowed to remove this cart item.");
            }

            await _cartItemRepository.DeleteAsync(cartItem);
            cart.TotalItems = cart.CartItems.Count - 1;
            await _cartRepository.UpdateAsync(cart);
        }

        public async Task<CartResponse> GetCartByCurrentCustomerOrCreateCartAsync()
        {
            var customer = await GetCustomerByCurrentUserService();

            var cart = await _cartRepository.GetByCustomerIdAsync(customer.Id.ToString());

            if (cart == null)
            {
                cart = new Cart()
                {
                    Customer = customer
                };
                await _cartRepository.AddAsync(cart);
            }

            return cart.ToResponse();
        }

        public async Task UpdateCartItemQuantityAsync(int itemId, UpdateCartItemQuantityRequest request)
        {
            var customer = await GetCustomerByCurrentUserService();

            var cart = await _cartRepository.GetByCustomerIdAsync(customer.Id.ToString());

            var cartItem = await _cartItemRepository.GetByIdAsync(itemId);
            if (cartItem == null)
            {
                throw new NotFoundException($"Cart item with ID {itemId} not found.");
            }

            if (cartItem.Cart.Id != cart.Id)
            {
                throw new UnauthorizedAccessException("User is not allowed to update this cart item.");
            }

            if (request.Quantity > cartItem.ProductVariant.StockQuantity)
            {
                throw new InvalidOperationException("Stock quantity of this item is insufficient.");
            }

            // Remove item if quantity is set to 0
            if (request.Quantity == 0)
            {
                await _cartItemRepository.DeleteAsync(cartItem);
                cart.TotalItems = cart.CartItems.Count - 1;
                await _cartRepository.UpdateAsync(cart);
                return;
            }

            cartItem.Quantity = request.Quantity;
            cartItem.TotalPrice = cartItem.Quantity * cartItem.UnitPrice;
            await _cartItemRepository.UpdateAsync(cartItem);
        }

        private async Task<Customer> GetCustomerByCurrentUserService()
        {
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

            return customer;
        }
    }
}
