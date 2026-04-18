namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.CancelSaleItem;

public class CancelSaleItemResponse
{
    public Guid SaleId { get; set; }
    public Guid ItemId { get; set; }
    public decimal SaleTotalAmount { get; set; }
}
