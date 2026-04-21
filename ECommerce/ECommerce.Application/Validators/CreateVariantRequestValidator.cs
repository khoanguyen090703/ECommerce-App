using ECommerce.Application.DTOs.Request;
using FluentValidation;

namespace ECommerce.Application.Validators
{
    public class CreateVariantRequestValidator : AbstractValidator<CreateVariantRequest>
    {
        public CreateVariantRequestValidator()
        {
            RuleFor(x => x.Price).GreaterThanOrEqualTo(0).WithMessage("Variant price must be >= 0.");
            RuleFor(x => x.Volumn).GreaterThanOrEqualTo(0).WithMessage("Variant volumn must be >= 0.");
            RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0).WithMessage("Variant stock quantity must be >= 0.");
            RuleFor(x => x.Images)
                .NotNull()
                .NotEmpty()
                .Must(list => list != null && list.Count <= 4)
                .WithMessage("Product just accept up to four images.");
        }
    }
}
