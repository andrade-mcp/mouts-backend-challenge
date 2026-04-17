using Ambev.DeveloperEvaluation.Domain.Entities;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;

public class CreateSaleRequestValidator : AbstractValidator<CreateSaleRequest>
{
    public CreateSaleRequestValidator()
    {
        RuleFor(r => r.SaleNumber).NotEmpty().MaximumLength(50);
        RuleFor(r => r.SaleDate).NotEqual(default(DateTime));
        RuleFor(r => r.CustomerId).NotEqual(Guid.Empty);
        RuleFor(r => r.CustomerName).NotEmpty().MaximumLength(200);
        RuleFor(r => r.BranchId).NotEqual(Guid.Empty);
        RuleFor(r => r.BranchName).NotEmpty().MaximumLength(200);
        RuleFor(r => r.Items).NotEmpty();
        RuleForEach(r => r.Items).SetValidator(new CreateSaleItemRequestValidator());
    }
}

public class CreateSaleItemRequestValidator : AbstractValidator<CreateSaleItemRequest>
{
    public CreateSaleItemRequestValidator()
    {
        RuleFor(i => i.ProductId).NotEqual(Guid.Empty);
        RuleFor(i => i.ProductName).NotEmpty().MaximumLength(200);
        RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0m);
        RuleFor(i => i.Quantity)
            .GreaterThan(0)
            .LessThanOrEqualTo(SaleItem.MaxQuantityPerItem);
    }
}
