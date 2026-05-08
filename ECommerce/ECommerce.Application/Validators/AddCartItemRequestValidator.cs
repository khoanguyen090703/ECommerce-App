using ECommerce.SharedViewModels.DTOs.Request;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Application.Validators
{
    public class AddCartItemRequestValidator : AbstractValidator<AddCartItemRequest>
    {
        public AddCartItemRequestValidator()
        {
            RuleFor(r => r.ProductVariantId)
                .GreaterThan(0).WithMessage("ProductVariantId must be greater than 0.");
        }
    }
}
