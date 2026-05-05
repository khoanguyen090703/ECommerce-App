using ECommerce.SharedViewModels.DTOs.Request;
using FluentValidation;
using System;

namespace ECommerce.Application.Validators
{
    public class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
    {
        public UpdateCategoryRequestValidator()
        {
            RuleFor(c => c.Name)
                .NotEmpty().WithMessage("Category name is required.");

            RuleFor(c => c.Description)
                .NotEmpty().WithMessage("Category description is required.");

            RuleFor(c => c.ImageUrl)
                .NotEmpty().WithMessage("Image URL is required.");

            RuleFor(c => c.ImageUrl)
                .MaximumLength(2048)
                .When(c => !string.IsNullOrWhiteSpace(c.ImageUrl));

            RuleFor(c => c.ImageUrl)
                .Must(BeValidHttpUrl)
                .When(c => !string.IsNullOrWhiteSpace(c.ImageUrl))
                .WithMessage("ImageUrl must be a valid absolute http(s) URL.");
        }

        private static bool BeValidHttpUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            return Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
    }
}
