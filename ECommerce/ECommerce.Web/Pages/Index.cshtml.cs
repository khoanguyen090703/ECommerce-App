using ECommerce.Web.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using ECommerce.SharedViewModels.DTOs.Response;

namespace ECommerce.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public IndexModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public List<CategoryResponse> Categories { get; set; } = new();
        public List<VariantResponse> FeaturedProducts { get; set; } = new();

        public async Task OnGetAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient(AuthConstants.ApiAnonymousClientName);
                var response = await client.GetAsync("api/categories/all");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Categories = JsonSerializer.Deserialize<List<CategoryResponse>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }

                var featuredResponse = await client.GetAsync("api/variants/featured");
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
