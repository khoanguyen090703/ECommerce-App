using FluentValidation;

namespace ECommerce.Tests.Application;

internal sealed class AlwaysValidValidator<T> : AbstractValidator<T>
{
}
