using Ambev.DeveloperEvaluation.Domain.Entities;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Domain.Validation;

public class SaleItemValidator : AbstractValidator<SaleItem>
{
    public SaleItemValidator()
    {
        RuleFor(i => i.ProductId).NotEqual(Guid.Empty).WithMessage("ProductId is required.");

        RuleFor(i => i.ProductName)
            .NotEmpty().WithMessage("ProductName is required.")
            .MaximumLength(200);

        RuleFor(i => i.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero.")
            .LessThanOrEqualTo(SaleItem.MaxQuantityPerItem)
                .WithMessage($"Cannot sell more than {SaleItem.MaxQuantityPerItem} identical items.");

        RuleFor(i => i.UnitPrice)
            .GreaterThanOrEqualTo(0m).WithMessage("UnitPrice cannot be negative.");
    }
}
