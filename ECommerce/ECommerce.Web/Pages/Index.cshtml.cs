using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace ECommerce.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public IndexModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public List<CategoryViewModel> Categories { get; set; } = new();
        public List<VariantViewModel> FeaturedProducts { get; set; } = new();

        public async Task OnGetAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var apiUrl = _configuration["ApiBaseUrl"] ?? "http://localhost:5206";
                var response = await client.GetAsync($"{apiUrl}/api/categories/all");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Categories = JsonSerializer.Deserialize<List<CategoryViewModel>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }

                var featuredResponse = await client.GetAsync($"{apiUrl}/api/variants/featured");
                if (featuredResponse.IsSuccessStatusCode)
                {
                    var featuredContent = await featuredResponse.Content.ReadAsStringAsync();
                    FeaturedProducts = JsonSerializer.Deserialize<List<VariantViewModel>>(featuredContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }
            }
            catch (Exception)
            {
                // Fallback or log error
            }
        }
    }

    public class CategoryViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class VariantViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public decimal Price { get; set; }
        public string Status { get; set; } = default!;
        public string ImageUrl { get; set; } = string.Empty;
    }
}
