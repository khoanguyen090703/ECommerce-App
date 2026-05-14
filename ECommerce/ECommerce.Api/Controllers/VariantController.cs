using ECommerce.Application.Interfaces;
using ECommerce.Domain.QueryParameters;
using ECommerce.SharedViewModels.DTOs.Request;
using Microsoft.AspNetCore.Authorization;
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

        /// <summary>List variants with status Available or OutOfStock (paging, search, optional status filter).</summary>
        [HttpGet("restock")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetRestockVariants([FromQuery] RestockVariantQueryParams parameters)
        {
            var paged = await _variantService.GetVariantsForStockRestockAsync(parameters);
            return Ok(paged);
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

        /// <summary>Basic variant info for restock UI (same shape as list rows).</summary>
        [HttpGet("{id:int:min(1)}/restock")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetRestockVariantById(int id)
        {
            var dto = await _variantService.GetVariantStockPanelByIdAsync(id);
            if (dto == null)
                return NotFound();
            return Ok(dto);
        }

        [HttpPut("{id:int:min(1)}")]
        public async Task<IActionResult> Update(int id, [FromBody] ECommerce.SharedViewModels.DTOs.Request.UpdateVariantRequest request)
        {
            if (id <= 0)
                return BadRequest("Invalid variant id.");

            await _variantService.UpdateVariantByIdAsync(id, request);
            return NoContent();
        }
        [HttpPost("product/{productId:int:min(1)}")]
        public async Task<IActionResult> Create(int productId, [FromBody] ECommerce.SharedViewModels.DTOs.Request.CreateVariantRequest request)
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

        /// <summary>Add stock to multiple variants (quantities are summed onto current stock).</summary>
        [HttpPost("restock")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PostRestock([FromBody] AddVariantStockBatchRequest request)
        {
            await _variantService.AddStockToVariantsAsync(request);
            return NoContent();
        }

        [HttpPatch("{id:int:min(1)}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] ECommerce.SharedViewModels.DTOs.Request.UpdateVariantStatusRequest request)
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
