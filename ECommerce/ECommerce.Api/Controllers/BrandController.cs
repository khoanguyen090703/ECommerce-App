using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers
{
    [ApiController]
    [Route("api/brands")]
    public class BrandController : Controller
    {
        private readonly IBrandService _brandService;

        public BrandController(IBrandService brandService)
        {
            _brandService = brandService;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var brands = await _brandService.GetAllBrandsAsync();
            return Ok(brands);
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] ECommerce.Domain.QueryParameters.BrandQueryParams parameters)
        {
            var paged = await _brandService.GetBrandsAsync(parameters);
            return Ok(paged);
        }
    }
}
