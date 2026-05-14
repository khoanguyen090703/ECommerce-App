namespace ECommerce.Web.Auth;

public static class AuthConstants
{
  public const string RefreshTokenCookieName = "rt";
  public const string AccessTokenClaimType = "AccessToken";
  public const string ApiClientName = "ApiClient";
  public const string ApiAnonymousClientName = "ApiAnonymous";

  public static readonly TimeSpan AuthenticationCookieLifetime = TimeSpan.FromDays(7);
  public static readonly TimeSpan RefreshTokenCookieLifetime = TimeSpan.FromDays(7);
}