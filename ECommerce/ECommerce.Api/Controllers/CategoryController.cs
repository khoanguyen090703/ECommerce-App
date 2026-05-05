using ECommerce.Application.Interfaces;
using ECommerce.Domain.QueryParameters;
using ECommerce.SharedViewModels.DTOs.Request;
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

        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _categoryService.GetAllAsync();
            return Ok(categories);
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] CategoryQueryParams parameters)
        {
            var paged = await _categoryService.GetCategoriesAsync(parameters);
            return Ok(paged);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            return Ok(category);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
        {
            await _categoryService.CreateProductAsync(request);
            return Created();
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update([FromBody] UpdateCategoryRequest request, int id)
        {
            await _categoryService.UpdateCategoryByIdAsync(id, request);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _categoryService.DeleteCategoryByIdAsync(id);
            return NoContent();
        }
    }
}
