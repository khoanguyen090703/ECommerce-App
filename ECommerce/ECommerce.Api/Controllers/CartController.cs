using ECommerce.Application.Interfaces;
using ECommerce.SharedViewModels.DTOs.Request;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers
{
    [ApiController]
    [Route("api/cart")]
    public class CartController : Controller
    {
        private readonly ILogger<CartController> _logger;

        private readonly ICartService _cartService;

        public CartController(ILogger<CartController> logger, ICartService cartService)
        {
            _logger = logger;
            _cartService = cartService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var response = await _cartService.GetCartByCurrentCustomerOrCreateCartAsync();
            return Ok(response);
        }

        [HttpPost("items")]
        public async Task<IActionResult> AddItemToCart([FromBody] AddCartItemRequest request)
        {
            await _cartService.AddItemToCartAsync(request);
            return Created();
        }

        [HttpPatch("items/{itemId:int:min(1)}")]
        public async Task<IActionResult> UpdateCartItem(int itemId, [FromBody] UpdateCartItemQuantityRequest request)
        {
            await _cartService.UpdateCartItemQuantityAsync(itemId, request);
            return Ok();
        }

        [HttpDelete("items/{itemId:int:min(1)}")]
        public async Task<IActionResult> RemoveItemFromCart(int itemId)
        {
            await _cartService.DeleteCartItemAsync(itemId);
            return Ok();
        }

        [HttpDelete("items")]
        public async Task<IActionResult> ClearCart()
        {
            await _cartService.ClearCartAsync();
            return Ok();
        }
    }
}
