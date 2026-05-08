using ECommerce.Application.Interfaces;
using ECommerce.SharedViewModels.DTOs.Request;
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
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            _logger.LogInformation("Creating a new order.");

            await _orderService.CreateOrderAsync(request);
            return Created();
        }

        [HttpGet("/api/checkout")]
        public async Task<IActionResult> GetCheckoutInfo()
        {
            var info = await _orderService.GetCheckoutInfoAsync();
            return Ok(info);
        }
    }
}
