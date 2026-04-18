using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

public class ListSalesValidator : AbstractValidator<ListSalesQuery>
{
    public const int MaxPageSize = 100;

    public ListSalesValidator()
    {
        RuleFor(q => q.Page).GreaterThan(0);
        RuleFor(q => q.PageSize).InclusiveBetween(1, MaxPageSize);

        RuleFor(q => q)
            .Must(q => !q.StartDate.HasValue || !q.EndDate.HasValue || q.StartDate <= q.EndDate)
            .WithMessage("StartDate must be on or before EndDate.");
    }
}
