using ECommerce.Application.Interfaces;
using ECommerce.Domain.QueryParameters;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers
{
    [ApiController]
    [Route("api/variants")]
    public class VariantController : Controller
    {
        private readonly ILogger<VariantController> _logger;
        private readonly IVariantService _variantService;

        public VariantController(ILogger<VariantController> logger, IVariantService variantService)
        {
            _logger = logger;
            _variantService = variantService;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var items = await _variantService.GetAllVariantsAsync();
            return Ok(items);
        }

        [HttpGet("featured")]
        public async Task<IActionResult> GetFeatured()
        {
            var items = await _variantService.GetFeaturedVariantsAsync();
            return Ok(items);
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] VariantQueryParams parameters)
        {
            var paged = await _variantService.GetVariantsAsync(parameters);
            return Ok(paged);
        }

        [HttpGet("{id:int:min(1)}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0)
                return BadRequest("Invalid variant id.");

            var dto = await _variantService.GetVariantDetailsByIdAsync(id);
            if (dto == null)
                return NotFound();

            return Ok(dto);
        }

        [HttpPut("{id:int:min(1)}")]
        public async Task<IActionResult> Update(int id, [FromBody] ECommerce.Application.DTOs.Request.UpdateVariantRequest request)
        {
            if (id <= 0)
                return BadRequest("Invalid variant id.");

            await _variantService.UpdateVariantByIdAsync(id, request);
            return NoContent();
        }
        [HttpPost("product/{productId:int:min(1)}")]
        public async Task<IActionResult> Create(int productId, [FromBody] ECommerce.Application.DTOs.Request.CreateVariantRequest request)
        {
            if (productId <= 0)
                return BadRequest("Invalid product id.");

            var newId = await _variantService.CreateVariantAsync(productId, request);
            return CreatedAtAction(nameof(GetById), new { id = newId }, null);
        }

        [HttpPost("featured")]
        public async Task<IActionResult> SetFeatured([FromBody] List<int> variantIds)
        {
            if (variantIds == null || !variantIds.Any())
                return BadRequest("variantIds is required.");

            await _variantService.SetFeaturedVariantsAsync(variantIds);
            return NoContent();
        }
        [HttpPatch("{id:int:min(1)}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] ECommerce.Application.DTOs.Request.UpdateVariantStatusRequest request)
        {
            if (id <= 0)
                return BadRequest("Invalid variant id.");

            await _variantService.UpdateVariantStatusByIdAsync(id, request.Status);
            return NoContent();
        }



        [HttpDelete("{id:int:min(1)}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest("Invalid variant id.");
            await _variantService.DeleteVariantByIdAsync(id);
            return NoContent();
        }
    }
}
