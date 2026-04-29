using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Interfaces;
using ECommerce.Infrastructure.Identity;
using ECommerce.Infrastructure.Services;
using ECommerce.SharedViewModels.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : Controller
    {
        private readonly ILogger<AuthController> _logger;

        private readonly IHttpContextAccessor _contextAccessor;

        private readonly IAuthService _authService;

        private readonly ICurrentUserService _currentUserService;

        public AuthController(
            ILogger<AuthController> logger,
            IHttpContextAccessor contextAccessor,
            IAuthService authService,
            ICurrentUserService currentUserService)
        {
            _logger = logger;
            _contextAccessor = contextAccessor;
            _authService = authService;
            _currentUserService = currentUserService;
        }

        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
        {
            var response = await _authService.SignUpAsync(request);

            if (!response.IsSuccess) 
                return BadRequest(response);
            return Ok(response);
        }

        [HttpPost("signin")]
        public async Task<IActionResult> SignIn([FromBody] SignInRequest request)
        {
            var response = await _authService.SignInAsync(request);

            if (!response.IsSuccess) 
                return Unauthorized(response);
            return Ok(response);
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                return BadRequest("Invalid request data.");

            var response = await _authService.ConfirmEmailAsync(userId, token);

            if (!response.IsSuccess) 
                return BadRequest(response);
            return Ok("Email confirmation success.");
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (string.IsNullOrEmpty(request.AccessToken) || string.IsNullOrEmpty(request.RefreshToken))
                return BadRequest("Token is required.");

            var response = await _authService.RefreshTokenAsync(request);

            // Trả về 401 để Client tự động log out nếu refresh token thất bại
            if (!response.IsSuccess) 
                return Unauthorized(response);
            return Ok(response);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = _currentUserService.UserId?.ToString();

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new AuthResponse 
                { 
                    IsSuccess = false, 
                    Message = "Unable to resolve the current user from the token."
                });

            var response = await _authService.LogoutAsync(userId);

            if (!response.IsSuccess)
                return BadRequest(response);

            return Ok(response);
        }

        [HttpPost("resend-confirmation")]
        [EnableRateLimiting("email-protection")]
        public async Task<IActionResult> ResendConfirmation([FromBody] ResendEmailConfirmationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest(new AuthResponse { IsSuccess = false, Message = "Email không được để trống." });

            var response = await _authService.ResendEmailConfirmationAsync(request);

            if (!response.IsSuccess)
                return BadRequest(response);

            return Ok(response);
        }
    }
}
