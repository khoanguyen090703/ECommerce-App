using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace ECommerce.Web.Pages.ProductVariants
{
    public class ProductVariantsListModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public ProductVariantsListModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [BindProperty(SupportsGet = true)]
        public string? Category { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Brand { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ScentFamily { get; set; }

        [BindProperty(SupportsGet = true)]
        public decimal? FromPrice { get; set; }

        [BindProperty(SupportsGet = true)]
        public decimal? ToPrice { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortBy { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 12;

        public PagedResult<VariantViewModel> VariantsData { get; set; } = new();
        public List<FilterItemViewModel> Categories { get; set; } = new();
        public List<FilterItemViewModel> Brands { get; set; } = new();
        public List<FilterItemViewModel> ScentFamilies { get; set; } = new();

        public async Task OnGetAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var apiUrl = _configuration["ApiBaseUrl"] ?? "http://localhost:5206";

                // Load filter options
                var catResponse = await client.GetAsync($"{apiUrl}/api/categories/all");
                if (catResponse.IsSuccessStatusCode)
                {
                    var content = await catResponse.Content.ReadAsStringAsync();
                    Categories = JsonSerializer.Deserialize<List<FilterItemViewModel>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }

                var brandResponse = await client.GetAsync($"{apiUrl}/api/brands/all");
                if (brandResponse.IsSuccessStatusCode)
                {
                    var content = await brandResponse.Content.ReadAsStringAsync();
                    var brandDtoList = JsonSerializer.Deserialize<List<BrandDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                    Brands = brandDtoList.Select(b => new FilterItemViewModel { Id = b.Id, Name = b.Name }).ToList();
                }

                var sfResponse = await client.GetAsync($"{apiUrl}/api/scentfamilies");
                if (sfResponse.IsSuccessStatusCode)
                {
                    var content = await sfResponse.Content.ReadAsStringAsync();
                    var sfDtoList = JsonSerializer.Deserialize<List<ScentFamilyDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                    ScentFamilies = sfDtoList.Select(sf => new FilterItemViewModel { Id = sf.Id, Name = sf.Name }).ToList();
                }

                // Build query string
                var queryParams = new List<string>();
                if (!string.IsNullOrEmpty(Category)) queryParams.Add($"Category={Uri.EscapeDataString(Category)}");
                if (!string.IsNullOrEmpty(Brand)) queryParams.Add($"Brand={Uri.EscapeDataString(Brand)}");
                if (!string.IsNullOrEmpty(ScentFamily)) queryParams.Add($"ScentFamily={Uri.EscapeDataString(ScentFamily)}");
                if (FromPrice.HasValue) queryParams.Add($"FromPrice={FromPrice.Value}");
                if (ToPrice.HasValue) queryParams.Add($"ToPrice={ToPrice.Value}");
                if (!string.IsNullOrEmpty(SortBy)) queryParams.Add($"SortBy={Uri.EscapeDataString(SortBy)}");
                
                // Ensure page number is valid
                if (PageNumber < 1) PageNumber = 1;

                queryParams.Add($"PageNumber={PageNumber}");
                queryParams.Add($"PageSize={PageSize}");

                var queryString = string.Join("&", queryParams);

                // Fetch variants
                var varResponse = await client.GetAsync($"{apiUrl}/api/variants?{queryString}");
                if (varResponse.IsSuccessStatusCode)
                {
                    var content = await varResponse.Content.ReadAsStringAsync();
                    VariantsData = JsonSerializer.Deserialize<PagedResult<VariantViewModel>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }
            }
            catch (Exception ex)
            {
                // Log exception
                Console.WriteLine(ex.Message);
            }
        }
    }

    // Intermediate DTOs to handle different shapes if any
    public class BrandDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
    }

    public class ScentFamilyDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
    }

    public class FilterItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
    }

    public class VariantViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public decimal Price { get; set; }
        public string Status { get; set; } = default!;
        public string ImageUrl { get; set; } = string.Empty;
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    }
}
