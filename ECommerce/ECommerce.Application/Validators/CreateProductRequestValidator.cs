using ECommerce.Application.DTOs.Request;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Application.Validators
{
    public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
    {
        public CreateProductRequestValidator()
        {
            // Name is generated on server; do not accept from client

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Product description is required.");

            RuleFor(x => x.Images)
                .NotEmpty().WithMessage("Product images are required.")
                .NotNull().WithMessage("Product images are required.")
                .Must(list => list != null && list.Count == 1)
                .WithMessage("Product must have exactly one image.");

            RuleFor(x => x.BrandId)
                .GreaterThan(0).WithMessage("Product's brand is required.");

            RuleFor(x => x.CategoryIds)
                .NotEmpty().WithMessage("At least one category is required.");

            RuleFor(x => x.Concentration)
                .IsInEnum().WithMessage("Invalid product concentration.");

            RuleForEach(x => x.Variants).SetValidator(new CreateProductVariantRequestValidator());
        }
    }

    public class CreateProductVariantRequestValidator : AbstractValidator<CreateProductVariantRequest>
    {
        public CreateProductVariantRequestValidator()
        {
            RuleFor(x => x.Price).GreaterThanOrEqualTo(0).WithMessage("Variant price must be >= 0.");
            RuleFor(x => x.Volumn).GreaterThanOrEqualTo(0).WithMessage("Variant volumn must be >= 0.");
            RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0).WithMessage("Variant stock quantity must be >= 0.");
            RuleFor(x => x.Images)
                .NotNull()
                .NotEmpty()
                .Must(list => list != null && list.Count <= 4)
                .WithMessage("Product just accept up to four images."); ;
            RuleFor(x => x.Format).IsInEnum().WithMessage("Invalid variant format.");
        }
    }
}
