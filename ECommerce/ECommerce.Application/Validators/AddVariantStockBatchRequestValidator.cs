using ECommerce.SharedViewModels.DTOs.Request;
using FluentValidation;
using System.Linq;

namespace ECommerce.Application.Validators
{
    public class AddVariantStockBatchRequestValidator : AbstractValidator<AddVariantStockBatchRequest>
    {
        public AddVariantStockBatchRequestValidator()
        {
            RuleFor(x => x.Items).NotNull().NotEmpty().WithMessage("At least one line item is required.");
            RuleForEach(x => x.Items).ChildRules(line =>
            {
                line.RuleFor(i => i.VariantId).GreaterThan(0);
                line.RuleFor(i => i.QuantityToAdd).GreaterThanOrEqualTo(1);
            });
            RuleFor(x => x.Items)
                .Must(items => items == null || !items.Any() || items.Select(i => i.VariantId).Distinct().Count() == items.Count)
                .WithMessage("Duplicate variant ids are not allowed.");
        }
    }
}
