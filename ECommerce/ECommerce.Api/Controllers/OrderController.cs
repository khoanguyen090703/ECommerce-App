using ECommerce.Application.Interfaces;
using ECommerce.Domain.QueryParameters;
using ECommerce.SharedViewModels.DTOs.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrderController : Controller
    {
        private readonly ILogger<OrderController> _logger;

        private readonly IOrderService _orderService;

        public OrderController(ILogger<OrderController> logger, IOrderService orderService)
        {
            _logger = logger;
            _orderService = orderService;
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            _logger.LogInformation("Creating a new order.");

            await _orderService.CreateOrderAsync(request);
            return Created();
        }

        [HttpGet("me")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetMyOrders([FromQuery] OrderQueryParams parameters)
        {
            var paged = await _orderService.GetMyOrdersAsync(parameters);
            return Ok(paged);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetOrders([FromQuery] OrderQueryParams parameters)
        {
            var paged = await _orderService.GetOrdersAsync(parameters);
            return Ok(paged);
        }

        [HttpGet("{id:int:min(1)}")]
        [Authorize]
        public async Task<IActionResult> GetOrderDetails(int id)
        {
            var details = await _orderService.GetOrderDetailsAsync(id);
            return Ok(details);
        }

        [HttpGet("/api/checkout")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetCheckoutInfo()
        {
            var info = await _orderService.GetCheckoutInfoAsync();
            return Ok(info);
        }
    }
}
