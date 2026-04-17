using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;

public class CancelSaleItemValidator : AbstractValidator<CancelSaleItemCommand>
{
    public CancelSaleItemValidator()
    {
        RuleFor(c => c.SaleId).NotEqual(Guid.Empty);
        RuleFor(c => c.ItemId).NotEqual(Guid.Empty);
    }
}
