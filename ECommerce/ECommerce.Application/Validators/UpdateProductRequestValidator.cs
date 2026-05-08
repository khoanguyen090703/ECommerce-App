using ECommerce.SharedViewModels.DTOs.Request;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Application.Validators
{
    public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
    {
        public UpdateProductRequestValidator()
        {
            RuleFor(c => c.Description)
                .NotEmpty().WithMessage("Product description is required.");

            RuleFor(c => c.BrandId)
                .GreaterThan(0).WithMessage("Product's brand id is required.");

            RuleFor(c => c.CategoryIds)
                .NotEmpty().WithMessage("At least one category id is required.");

            RuleFor(c => c.ScentFamilyIds)
                .NotEmpty().WithMessage("At least one scent family is required.");

            RuleFor(c => c.Images)
                .NotNull()
                .Must(list => list is { Count: 1 })
                .WithMessage("Product must have exactly one image.");

            RuleFor(c => c.Concentration)
                .IsInEnum().WithMessage("Invalid product concentration.");
        }
    }
}
