using ECommerce.Application.DTOs.Request;
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
            RuleFor(c => c.Name)
                .NotEmpty().WithMessage("Product name is required.");

            RuleFor(c => c.Description)
                .NotEmpty().WithMessage("Product description is required.");

            RuleFor(c => c.Price)
                .NotEmpty().WithMessage("Product price is required.");

            RuleFor(c => c.CategoryId)
                .NotEmpty().WithMessage("Product's category id is required.");
        }
    }
}
