using ECommerce.Application.DTOs.Request;
using ECommerce.Application.Interfaces;
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

        [HttpGet]
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

        [HttpGet("{id:int:min(1)}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0)
                return BadRequest("Invalid product id.");
            var products = await _productService.GetProductByIdAsync(id);
            return Ok(products);
        }

        [HttpPut("{id:int:min(1)}")]
        public async Task<IActionResult> Update([FromBody] UpdateProductRequest request, int id)
        {
            await _productService.UpdateProductByIdAsync(id, request);
            return NoContent();
        }

        [HttpDelete("{id:int:min(1)}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _productService.DeleteProductByIdAsync(id);
            return NoContent();
        }
    }
}
