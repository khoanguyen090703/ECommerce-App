using Azure.Core;
using ECommerce.Application.DTOs.Request;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers
{
    [ApiController]
    [Route("api/categories")]
    public class CategoryController : Controller
    {
        private readonly ILogger<CategoryController> _logger;

        private readonly ICategoryService _categoryService;

        public CategoryController(ILogger<CategoryController> logger, ICategoryService categoryService)
        {
            _logger = logger;
            _categoryService = categoryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _categoryService.GetAllAsync();
            return Ok(categories);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
        {
            await _categoryService.CreateProductAsync(request);
            return Created();
        }

        [HttpPut("/{id:int}")]
        public async Task<IActionResult> Update([FromBody] UpdateCategoryRequest request, int id)
        {
            await _categoryService.UpdateCategoryByIdAsync(id, request);
            return NoContent();
        }

        [HttpDelete("/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _categoryService.DeleteCategoryByIdAsync(id);
            return NoContent();
        }
    }
}
