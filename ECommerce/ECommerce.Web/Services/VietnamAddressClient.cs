using System.Net.Http.Json;
using System.Text.Json;
using ECommerce.Web.Models;

namespace ECommerce.Web.Services;

public sealed class VietnamAddressClient
{
  private const string BaseUrl = "https://provinces.open-api.vn/api/";

  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNameCaseInsensitive = true
  };

  private readonly IHttpClientFactory _httpClientFactory;

  public VietnamAddressClient(IHttpClientFactory httpClientFactory)
  {
    _httpClientFactory = httpClientFactory;
  }

  public async Task<IReadOnlyList<VietnamRegionOption>> GetProvincesAsync(CancellationToken cancellationToken)
  {
    var client = _httpClientFactory.CreateClient();
    using var response = await client.GetAsync($"{BaseUrl}p/", cancellationToken);
    response.EnsureSuccessStatusCode();

    var rows = await response.Content.ReadFromJsonAsync<List<ProvinceApiRow>>(JsonOptions, cancellationToken);
    return (rows ?? [])
    .Where(row => row.Code > 0 && !string.IsNullOrWhiteSpace(row.Name))
    .Select(row => new VietnamRegionOption(row.Code, row.Name.Trim()))
    .ToList();
  }

  public async Task<IReadOnlyList<VietnamRegionOption>> GetDistrictsAsync(int provinceCode, CancellationToken cancellationToken)
  {
    if (provinceCode < 1)
      return [];

    var client = _httpClientFactory.CreateClient();
    using var response = await client.GetAsync($"{BaseUrl}p/{provinceCode}?depth=2", cancellationToken);
    response.EnsureSuccessStatusCode();

    var payload = await response.Content.ReadFromJsonAsync<ProvinceWithDistrictsApiRow>(JsonOptions, cancellationToken);
    return (payload?.Districts ?? [])
      .Where(row => row.Code > 0 && !string.IsNullOrWhiteSpace(row.Name))
      .Select(row => new VietnamRegionOption(row.Code, row.Name.Trim()))
      .ToList();
  }

  public async Task<IReadOnlyList<VietnamRegionOption>> GetWardsAsync(int districtCode, CancellationToken cancellationToken)
  {
    if (districtCode < 1)
      return [];

    var client = _httpClientFactory.CreateClient();
    using var response = await client.GetAsync($"{BaseUrl}d/{districtCode}?depth=2", cancellationToken);
    response.EnsureSuccessStatusCode();

    var payload = await response.Content.ReadFromJsonAsync<DistrictWithWardsApiRow>(JsonOptions, cancellationToken);
    return (payload?.Wards ?? [])
      .Where(row => row.Code > 0 && !string.IsNullOrWhiteSpace(row.Name))
      .Select(row => new VietnamRegionOption(row.Code, row.Name.Trim()))
      .ToList();
  }

  private sealed class ProvinceApiRow
  {
    public int Code { get; set; }

    public string Name { get; set; } = string.Empty;
  }

  private sealed class ProvinceWithDistrictsApiRow
  {
    public List<ProvinceApiRow> Districts { get; set; } = [];
  }

  private sealed class DistrictWithWardsApiRow
  {
    public List<ProvinceApiRow> Wards { get; set; } = [];
  }
}
