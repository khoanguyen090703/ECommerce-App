using System.Text.Json;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Pages.ProductVariants
{
    public class ProductDetailsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public ProductDetailsModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public VariantDetails4CusViewModel? VariantDetails { get; set; }
        public int CurrentVariantId { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync(int id)
        {
            CurrentVariantId = id;
            if (id <= 0)
            {
                ErrorMessage = "Id variant không hợp lệ.";
                return;
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var apiUrl = _configuration["ApiBaseUrl"] ?? "http://localhost:5206";
                var response = await client.GetAsync($"{apiUrl}/api/products/variant/{id}");

                if (!response.IsSuccessStatusCode)
                {
                    ErrorMessage = "Không thể tải thông tin chi tiết sản phẩm.";
                    return;
                }

                var content = await response.Content.ReadAsStringAsync();
                VariantDetails = JsonSerializer.Deserialize<VariantDetails4CusViewModel>(
                    content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (VariantDetails is null)
                {
                    ErrorMessage = "Không tìm thấy dữ liệu chi tiết sản phẩm.";
                }
            }
            catch (Exception)
            {
                ErrorMessage = "Đã xảy ra lỗi khi tải chi tiết sản phẩm.";
            }
        }
    }

    public class VariantDetails4CusViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Categories { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int SoldQuantity { get; set; }
        public decimal Price { get; set; }
        public List<string> ImageUrls { get; set; } = new();
        public List<Variant4CusProdDetailsViewModel> ProductVariants { get; set; } = new();
    }

    public class Variant4CusProdDetailsViewModel
    {
        public int Id { get; set; }
        public string Format { get; set; } = string.Empty;
        public string Volumn { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }
}
