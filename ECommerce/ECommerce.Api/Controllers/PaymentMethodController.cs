using ECommerce.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers
{
    [ApiController]
    [Route("api/payment-methods")]
    public class PaymentMethodController : ControllerBase
    {
        private readonly IPaymentMethodService _service;

        public PaymentMethodController(IPaymentMethodService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
        {
            var list = await _service.GetAllAsync(includeInactive);
            return Ok(list);
        }
    }
}
