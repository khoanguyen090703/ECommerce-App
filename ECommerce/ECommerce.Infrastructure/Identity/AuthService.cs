using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.SharedViewModels.DTOs.Auth;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace ECommerce.Infrastructure.Identity
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<AppUser> _userManager;

        private readonly ITokenService _tokenService;

        private readonly IEmailService _emailService;

        private readonly IUnitOfWork _unitOfWork;

        private readonly ICustomerRepository _customerRepository;

        public AuthService(
            UserManager<AppUser> userManager,
            ITokenService tokenService,
            IEmailService emailService,
            IUnitOfWork unitOfWork,
            ICustomerRepository customerRepository)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _emailService = emailService;
            _unitOfWork = unitOfWork;
            _customerRepository = customerRepository;
        }

        public async Task<AuthResponse> ConfirmEmailAsync(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return new AuthResponse
                {
                    IsSuccess = false,
                    Message = "User not found."
                };

            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

            if (result.Succeeded)
                return new AuthResponse
                {
                    IsSuccess = true,
                    Message = "Email confirmation success."
                };

            return new AuthResponse
            {
                IsSuccess = false,
                Message = "Email confirmation failed."
            };
        }

        public async Task<AuthResponse> LogoutAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return new AuthResponse 
                { 
                    IsSuccess = false, 
                    Message = "User not found." 
                };

            user.RefreshToken = null;

            // Cập nhật lại thời gian hết hạn về thời điểm hiện tại hoặc trong quá khứ
            user.RefreshTokenExpiryTime = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
                return new AuthResponse 
                { 
                    IsSuccess = true, 
                    Message = "Log out successfully." 
                };

            return new AuthResponse
            {
                IsSuccess = false,
                Message = "Error when logging out."
            };
        }

        public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
            if (principal == null)
                return new AuthResponse
                {
                    IsSuccess = false,
                    Message = "Invalid access token."
                };

            var email = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var user = await _userManager.FindByEmailAsync(email!);

            if (user == null
                || user.RefreshToken != request.RefreshToken
                || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                return new AuthResponse
                {
                    IsSuccess = false,
                    Message = "Invalid refresh token or refresh token is expired."
                };

            var roles = await _userManager.GetRolesAsync(user);
            var newJwtToken = _tokenService.GenerateJwtToken(user.Id.ToString(), user.Email!, roles);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userManager.UpdateAsync(user);

            return new AuthResponse
            {
                IsSuccess = true,
                Message = "Refresh token success.",
                RefreshToken = newRefreshToken,
                Token = newJwtToken
            };
        }

        public async Task<AuthResponse> SignInAsync(SignInRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
                return new AuthResponse
                {
                    IsSuccess = false,
                    Message = "Username or password is not correct."
                };

            if (!await _userManager.IsEmailConfirmedAsync(user))
                return new AuthResponse
                {
                    IsSuccess = false,
                    Message = "Email is not confirmed. Please check email inbox."
                };

            var roles = await _userManager.GetRolesAsync(user);
            var jwtToken = _tokenService.GenerateJwtToken(user.Id.ToString(), user.Email!, roles);

            // Create and save refresh token
            var refreshToken = _tokenService.GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userManager.UpdateAsync(user);

            return new AuthResponse
            {
                IsSuccess = true,
                Message = "Sign in successfully.",
                Token = jwtToken,
                RefreshToken = refreshToken
            };
        }

        public async Task<AuthResponse> SignUpAsync(SignUpRequest request, string originUrl)
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if(existingUser != null)
            {
                return new AuthResponse
                {
                    IsSuccess = false,
                    Message = "Email is already registered."
                };
            }

            var user = new AppUser
            {
                UserName = request.Email,
                Email = request.Email
            };

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var result = await _userManager.CreateAsync(user, request.Password);
                if (!result.Succeeded)
                {
                    return new AuthResponse
                    {
                        IsSuccess = false,
                        Message = "Failed to create user: " + string.Join(", ", result.Errors.Select(e => e.Description))
                    };
                }

                var roleResult = await _userManager.AddToRoleAsync(user, "Customer");
                if (!roleResult.Succeeded)
                {
                    // Xử lý nếu lỗi gán role (tuỳ bạn quyết định có rollback hay không)
                    return new AuthResponse 
                    { 
                        IsSuccess = false, 
                        Message = "User is created successfully but assign role failed." 
                    };
                }

                var customer = new Customer
                {
                    Id = Guid.NewGuid(),
                    FullName = request.FullName,
                    IdentityId = user.Id
                };
                await _customerRepository.AddAsync(customer);

                await _unitOfWork.CommitTransactionAsync();
            }
            catch(Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new AuthResponse
                {
                    IsSuccess = false,
                    Message = "User is created failed."
                };
            }
            
            // Handle send email confirmation
            await SendEmailConfirmation(user, originUrl);

            return new AuthResponse
            {
                IsSuccess = true,
                Message = "Sign up successfully! Please check email for confirmation."
            };
        }

        private async Task SendEmailConfirmation(AppUser user, string originUrl)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var confirmationLink = $"{originUrl}/api/auth/confirm-email?userId={user.Id}&token={encodedToken}";
            var emailBody = $"Vui lòng xác nhận tài khoản bằng cách click vào link: <a href='{confirmationLink}'>Xác nhận Email</a>";

            await _emailService.SendEmailAsync(user.Email!, "Xác nhận đăng ký tài khoản", emailBody);
        }
    }
}
