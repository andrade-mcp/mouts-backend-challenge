namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

/// <summary>Identity of the newly created sale.</summary>
public class CreateSaleResult
{
    public Guid Id { get; set; }
    public string SaleNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}
