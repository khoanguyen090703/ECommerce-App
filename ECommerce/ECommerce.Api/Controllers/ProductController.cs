using ECommerce.SharedViewModels.DTOs.Request;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.QueryParameters;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductController : Controller
    {
        private readonly ILogger<ProductController> _logger;

        private readonly IProductService _productService;

        public ProductController(ILogger<ProductController> logger, IProductService productService)
        {
            _logger = logger;
            _productService = productService;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var products = await _productService.GetAllAsync();
            return Ok(products);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
        {
            await _productService.AddAsync(request);
            return Created();
        }

        /// <param name="includeVariants">When false, <c>variants</c> in the response is empty; load variants via <c>GET .../variants</c> instead.</param>
        [HttpGet("{id:int:min(1)}")]
        public async Task<IActionResult> GetById(int id, [FromQuery] bool includeVariants = true)
        {
            if (id <= 0)
                return BadRequest("Invalid product id.");
            var products = await _productService.GetProductByIdAsync(id, includeVariants);
            return Ok(products);
        }

        [HttpGet("{productId:int:min(1)}/variants")]
        public async Task<IActionResult> GetProductVariants(int productId, [FromQuery] ProductVariantsQueryParams parameters)
        {
            if (productId <= 0)
                return BadRequest("Invalid product id.");
            var paged = await _productService.GetProductVariantsByProductIdAsync(productId, parameters);
            return Ok(paged);
        }

        [HttpGet("variant/{variantId:int:min(1)}")]
        public async Task<IActionResult> GetVariantsByVariantId(int variantId)
        {
            if (variantId <= 0)
                return BadRequest("Invalid variant id.");

            var resp = await _productService.GetProductWithVariantsByVariantIdAsync(variantId);
            return Ok(resp);
        }

        [HttpPut("{id:int:min(1)}")]
        public async Task<IActionResult> Update([FromBody] UpdateProductRequest request, int id)
        {
            await _productService.UpdateProductByIdAsync(id, request);
            return NoContent();
        }

        [HttpPatch("{id:int:min(1)}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] ECommerce.SharedViewModels.DTOs.Request.UpdateProductStatusRequest request)
        {
            if (id <= 0)
                return BadRequest("Invalid product id.");

            await _productService.UpdateProductStatusAsync(id, request.Status);
            return NoContent();
        }

        [HttpDelete("{id:int:min(1)}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _productService.DeleteProductByIdAsync(id);
            return NoContent();
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] ProductQueryParams parameters)
        {
            var products = await _productService.GetProductsAsync(parameters);
            return Ok(products);
        }
    }
}
