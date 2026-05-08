using ECommerce.SharedViewModels.DTOs.Request;
using FluentValidation;

namespace ECommerce.Application.Validators
{
    public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
    {
        public CreateOrderRequestValidator()
        {
            RuleFor(x => x.RecipientName)
                .NotEmpty().WithMessage("Recipient name is required.")
                .MaximumLength(200).WithMessage("Recipient name must be at most 200 characters.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required.")
                .Matches("^[0-9+\\- ]+$").WithMessage("Phone number contains invalid characters.")
                .MinimumLength(7).WithMessage("Phone number is too short.")
                .MaximumLength(20).WithMessage("Phone number is too long.");

            RuleFor(x => x.ShippingAddress)
                .NotEmpty().WithMessage("Shipping address is required.")
                .MaximumLength(1000).WithMessage("Shipping address is too long.");

            RuleFor(x => x.PaymentMethodId)
                .GreaterThan(0).WithMessage("Payment method is required.");

            RuleFor(x => x.OrderItems)
                .NotNull().WithMessage("Order items are required.")
                .NotEmpty().WithMessage("At least one order item is required.");

            RuleForEach(x => x.OrderItems).SetValidator(new Item4CreateOrderRequestValidator());
        }
    }

    public class Item4CreateOrderRequestValidator : AbstractValidator<Item4CreateOrderRequest>
    {
        public Item4CreateOrderRequestValidator()
        {
            RuleFor(x => x.ProductVariantId)
                .GreaterThan(0).WithMessage("Product variant id is required.");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be at least 1.")
                .LessThanOrEqualTo(1000).WithMessage("Quantity is too large.");
        }
    }
}
