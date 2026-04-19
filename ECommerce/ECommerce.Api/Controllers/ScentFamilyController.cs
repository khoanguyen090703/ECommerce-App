using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers
{
    [ApiController]
    [Route("api/scentfamilies")]
    public class ScentFamilyController : Controller
    {
        private readonly ILogger<ScentFamilyController> _logger;
        private readonly IScentFamilyService _scentFamilyService;

        public ScentFamilyController(ILogger<ScentFamilyController> logger, IScentFamilyService scentFamilyService)
        {
            _logger = logger;
            _scentFamilyService = scentFamilyService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _scentFamilyService.GetAllScentFamiliesAsync();
            return Ok(items);
        }
    }
}
