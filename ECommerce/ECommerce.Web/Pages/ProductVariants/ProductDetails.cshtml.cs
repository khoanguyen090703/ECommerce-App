using System.Text.Json;
using ECommerce.SharedViewModels.DTOs.Response;
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

        public VariantDetails4Cus? VariantDetails { get; set; }
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
                VariantDetails = JsonSerializer.Deserialize<VariantDetails4Cus>(
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

}
