using Ambev.DeveloperEvaluation.Domain.Entities;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

public class CreateSaleValidator : AbstractValidator<CreateSaleCommand>
{
    public CreateSaleValidator()
    {
        RuleFor(c => c.SaleNumber).NotEmpty().MaximumLength(50);
        RuleFor(c => c.SaleDate).NotEqual(default(DateTime));

        RuleFor(c => c.CustomerId).NotEqual(Guid.Empty);
        RuleFor(c => c.CustomerName).NotEmpty().MaximumLength(200);

        RuleFor(c => c.BranchId).NotEqual(Guid.Empty);
        RuleFor(c => c.BranchName).NotEmpty().MaximumLength(200);

        RuleFor(c => c.Items).NotEmpty().WithMessage("A sale must contain at least one item.");
        RuleForEach(c => c.Items).SetValidator(new CreateSaleItemValidator());
    }
}

public class CreateSaleItemValidator : AbstractValidator<CreateSaleItemCommand>
{
    public CreateSaleItemValidator()
    {
        RuleFor(i => i.ProductId).NotEqual(Guid.Empty);
        RuleFor(i => i.ProductName).NotEmpty().MaximumLength(200);
        RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0m);
        RuleFor(i => i.Quantity)
            .GreaterThan(0)
            .LessThanOrEqualTo(SaleItem.MaxQuantityPerItem)
                .WithMessage($"Cannot sell more than {SaleItem.MaxQuantityPerItem} identical items.");
    }
}
