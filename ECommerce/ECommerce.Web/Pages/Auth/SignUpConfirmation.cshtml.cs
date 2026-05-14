using ECommerce.SharedViewModels.DTOs.Auth;
using ECommerce.Web.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace ECommerce.Web.Pages.Auth;

public class SignUpConfirmationModel : PageModel
{
  private const string CooldownSessionKeyPrefix = "EmailResendUntilUtc:";
  private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);

  private readonly IHttpClientFactory _httpClientFactory;

  public SignUpConfirmationModel(IHttpClientFactory httpClientFactory)
  {
    _httpClientFactory = httpClientFactory;
  }

  public string Email { get; private set; } = string.Empty;

  public int ResendCooldownSeconds { get; private set; }

  [TempData]
  public string? StatusMessage { get; set; }

  [TempData]
  public bool StatusIsError { get; set; }

  [TempData]
  public bool RestartResendCooldown { get; set; }

  public IActionResult OnGet()
  {
    var email = HttpContext.Session.GetString("PendingConfirmationEmail");
    if (string.IsNullOrWhiteSpace(email))
      return RedirectToPage("/Auth/SignUp");

    Email = email;

    if (HttpContext.Session.GetString("EmailConfirmNeedsCooldown") == "1")
    {
      SetResendCooldown(email);
      HttpContext.Session.Remove("EmailConfirmNeedsCooldown");
    }

    if (RestartResendCooldown)
      SetResendCooldown(email);

    ResendCooldownSeconds = GetResendCooldownSeconds(email);
    return Page();
  }

  public async Task<IActionResult> OnPostResendAsync()
  {
    var email = HttpContext.Session.GetString("PendingConfirmationEmail");
    if (string.IsNullOrWhiteSpace(email))
      return RedirectToPage("/Auth/SignUp");

    Email = email;

    if (GetResendCooldownSeconds(email) > 0)
    {
      StatusMessage = "Vui lòng đợi trước khi gửi lại email xác nhận.";
      StatusIsError = true;
      return RedirectToPage();
    }

    try
    {
      var client = _httpClientFactory.CreateClient(AuthConstants.ApiAnonymousClientName);
      var body = new ResendEmailConfirmationRequest { Email = email };
      var response = await client.PostAsJsonAsync("api/auth/resend-confirmation", body);
      var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();

      if (!response.IsSuccessStatusCode || authResponse?.IsSuccess != true)
      {
        StatusMessage = authResponse?.Message ?? "Không gửi lại được email. Vui lòng thử sau.";
        StatusIsError = true;
        RestartResendCooldown = false;
      }
      else
      {
        StatusMessage = "Đã gửi lại email xác nhận. Vui lòng kiểm tra hộp thư.";
        StatusIsError = false;
        RestartResendCooldown = true;
      }
    }
    catch
    {
      StatusMessage = "Không kết nối được máy chủ. Vui lòng thử lại sau.";
      StatusIsError = true;
      RestartResendCooldown = false;
    }

    return RedirectToPage();
  }

  private void SetResendCooldown(string email)
  {
    var until = DateTimeOffset.UtcNow.Add(ResendCooldown);
    HttpContext.Session.SetString(CooldownSessionKeyPrefix + email, until.ToString("O"));
  }

  private int GetResendCooldownSeconds(string email)
  {
    var raw = HttpContext.Session.GetString(CooldownSessionKeyPrefix + email);
    if (!DateTimeOffset.TryParse(raw, out var until))
      return 0;

    var seconds = (int)Math.Ceiling((until - DateTimeOffset.UtcNow).TotalSeconds);
    return Math.Max(0, seconds);
  }
}
