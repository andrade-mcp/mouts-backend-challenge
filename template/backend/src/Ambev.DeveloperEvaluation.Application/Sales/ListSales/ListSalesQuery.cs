using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

public class ListSalesQuery : IRequest<ListSalesResult>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SaleNumber { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? BranchId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IncludeCancelled { get; set; } = true;
    public string? OrderBy { get; set; }
}
