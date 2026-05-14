using System.Globalization;

namespace ECommerce.Web.Helpers;

public static class WebDisplay
{
  private static readonly CultureInfo VietnameseCulture = CultureInfo.GetCultureInfo("vi-VN");

  public static string FormatCurrency(decimal value)
    => string.Format(VietnameseCulture, "{0:N0} ₫", value);

  public static string FormatDate(DateTime? value)
  {
    if (value is null)
      return "—";

    return value.Value.ToLocalTime().ToString("g", VietnameseCulture);
  }

  public static string FormatPaymentStatus(string? status)
  {
    return (status ?? string.Empty).ToLowerInvariant() switch
    {
      "unpaid" => "Chưa thanh toán",
      "paid" => "Đã thanh toán",
      "partiallyrefunded" => "Hoàn tiền một phần",
      "fullyrefunded" => "Đã hoàn tiền",
      "failed" => "Thanh toán thất bại",
      "pending" => "Đang chờ thanh toán",
      "completed" => "Đã thanh toán",
      _ => string.IsNullOrWhiteSpace(status) ? "—" : status
    };
  }
}
