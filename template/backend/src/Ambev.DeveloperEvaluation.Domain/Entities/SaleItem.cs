using Ambev.DeveloperEvaluation.Common.Validation;
using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Validation;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

/// <summary>Line item of a <see cref="Sale"/>; discount and total are derived, never accepted from the client.</summary>
public class SaleItem : BaseEntity
{
    public const int MaxQuantityPerItem = 20;
    public const int DiscountTier1MinQuantity = 4;
    public const int DiscountTier2MinQuantity = 10;
    public const decimal DiscountTier1Rate = 0.10m;
    public const decimal DiscountTier2Rate = 0.20m;

    public Guid SaleId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Discount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public bool IsCancelled { get; private set; }

    protected SaleItem() { }

    public SaleItem(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        ProductName = productName;
        SetQuantityAndPrice(quantity, unitPrice);
    }

    /// <summary>Updates quantity and price together; both feed the discount tier and line total.</summary>
    public void SetQuantityAndPrice(int quantity, decimal unitPrice)
    {
        Quantity = quantity;
        UnitPrice = unitPrice;
        Discount = ResolveDiscount(quantity);
        TotalAmount = decimal.Round(quantity * unitPrice * (1m - Discount), 2, MidpointRounding.AwayFromZero);
    }

    public void Cancel()
    {
        IsCancelled = true;
        TotalAmount = 0m;
    }

    public ValidationResultDetail Validate()
    {
        var validator = new SaleItemValidator();
        var result = validator.Validate(this);
        return new ValidationResultDetail
        {
            IsValid = result.IsValid,
            Errors = result.Errors.Select(o => (ValidationErrorDetail)o)
        };
    }

    /// <summary>
    /// Discount tiers per README: &lt;4 → 0%, 4–9 → 10%, 10–20 → 20%.
    /// &gt;20 throws as an invariant guard; <see cref="SaleItemValidator"/> blocks it first as a 400.
    /// </summary>
    private static decimal ResolveDiscount(int quantity)
    {
        if (quantity < DiscountTier1MinQuantity) return 0m;
        if (quantity < DiscountTier2MinQuantity) return DiscountTier1Rate;
        if (quantity <= MaxQuantityPerItem) return DiscountTier2Rate;
        throw new DomainException($"Cannot sell more than {MaxQuantityPerItem} identical items.");
    }
}
