using ECommerce.SharedViewModels.DTOs.Request;
using FluentValidation;

namespace ECommerce.Application.Validators
{
    public class UpdateVariantRequestValidator : AbstractValidator<UpdateVariantRequest>
    {
        public UpdateVariantRequestValidator()
        {
            RuleFor(x => x.Price).GreaterThanOrEqualTo(0).WithMessage("Variant price must be >= 0.");
            RuleFor(x => x.Volumn).GreaterThanOrEqualTo(0).WithMessage("Variant volumn must be >= 0.");
            RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0).WithMessage("Variant stock quantity must be >= 0.");
            RuleFor(x => x.ImageUrls)
                .NotNull()
                .NotEmpty()
                .Must(list => list != null && list.Count <= 4)
                .WithMessage("Product just accept up to four images.");
        }
    }
}
