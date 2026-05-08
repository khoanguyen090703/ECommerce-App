namespace ECommerce.SharedViewModels.DTOs.Response;

/// <summary>
/// Lightweight cart summary for badges and polling (Bearer token only, no body).
/// </summary>
public sealed class CartItemCountResponse
{
    public int TotalItems { get; set; }
}
