using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using ECommerce.SharedViewModels.DTOs.Response;

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

        public List<CategoryResponse> Categories { get; set; } = new();
        public List<VariantResponse> FeaturedProducts { get; set; } = new();

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
                    Categories = JsonSerializer.Deserialize<List<CategoryResponse>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }

                var featuredResponse = await client.GetAsync($"{apiUrl}/api/variants/featured");
                if (featuredResponse.IsSuccessStatusCode)
                {
                    var featuredContent = await featuredResponse.Content.ReadAsStringAsync();
                    FeaturedProducts = JsonSerializer.Deserialize<List<VariantResponse>>(featuredContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }
            }
            catch (Exception)
            {
                // Fallback or log error
            }
        }
    }

}
