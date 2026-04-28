using System.Security.Claims;

namespace ECommerce.Application.Common.Interfaces;

public interface ITokenService {
    string GenerateJwtToken(string userId, string email, IList<string> roles);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}