using ECommerce.Web.Auth;
using System.Text.Json;
using ECommerce.SharedViewModels.DTOs.Response;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Pages.ProductVariants
{
    public class ProductDetailsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProductDetailsModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public VariantDetails4Cus? VariantDetails { get; set; }
        public int CurrentVariantId { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync(int id)
        {
            CurrentVariantId = id;
            if (id <= 0)
            {
                ErrorMessage = "Mã biến thể không hợp lệ.";
                return;
            }

            try
            {
                var client = _httpClientFactory.CreateClient(AuthConstants.ApiAnonymousClientName);
                var response = await client.GetAsync($"api/products/variant/{id}");

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
