using ECommerce.Application.Interfaces;
using ECommerce.Domain.QueryParameters;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers
{
    [ApiController]
    [Route("api/customers")]
    public class CustomerController : Controller
    {
        private readonly ICustomerService _customerService;

        public CustomerController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] CustomerQueryParams parameters)
        {
            var paged = await _customerService.GetCustomersAsync(parameters);
            return Ok(paged);
        }
    }
}
