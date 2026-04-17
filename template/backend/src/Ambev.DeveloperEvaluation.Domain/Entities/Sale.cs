using Ambev.DeveloperEvaluation.Common.Validation;
using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Validation;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

/// <summary>Aggregate root for a sale; customer/branch/product use External Identities.</summary>
public class Sale : BaseEntity
{
    private readonly List<SaleItem> _items = new();
    private readonly List<DomainEvent> _domainEvents = new();

    public string SaleNumber { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public decimal TotalAmount { get; private set; }
    public bool IsCancelled { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>Optimistic concurrency token; mismatch surfaces as <c>DbUpdateConcurrencyException</c>.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public IReadOnlyCollection<SaleItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public Sale()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>Adds a line, applies the discount tier, and recalculates total.</summary>
    public SaleItem AddItem(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        EnsureNotCancelled();
        var item = new SaleItem(productId, productName, quantity, unitPrice) { SaleId = Id };
        _items.Add(item);
        RecalculateTotal();
        return item;
    }

    /// <summary>Full-replacement semantics for Update; one modified event instead of per-line diffs.</summary>
    public void ReplaceItems(IEnumerable<(Guid productId, string productName, int quantity, decimal unitPrice)> items)
    {
        EnsureNotCancelled();
        _items.Clear();
        foreach (var (productId, productName, quantity, unitPrice) in items)
        {
            _items.Add(new SaleItem(productId, productName, quantity, unitPrice) { SaleId = Id });
        }
        RecalculateTotal();
        Touch();
        RaiseEvent(new SaleModifiedEvent(Id, SaleNumber, TotalAmount, DateTime.UtcNow));
    }

    public void CancelItem(Guid itemId)
    {
        EnsureNotCancelled();

        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new DomainException($"Item {itemId} not found on sale {Id}.");

        if (item.IsCancelled)
            throw new DomainException($"Item {itemId} is already cancelled.");

        item.Cancel();
        RecalculateTotal();
        Touch();
        RaiseEvent(new ItemCancelledEvent(Id, item.Id, item.ProductId, item.Quantity, DateTime.UtcNow));
    }

    public void Cancel()
    {
        if (IsCancelled)
            throw new DomainException($"Sale {Id} is already cancelled.");

        IsCancelled = true;
        Touch();
        RaiseEvent(new SaleCancelledEvent(Id, SaleNumber, DateTime.UtcNow));
    }

    public void MarkCreated()
    {
        RaiseEvent(new SaleCreatedEvent(Id, SaleNumber, TotalAmount, DateTime.UtcNow));
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    public ValidationResultDetail Validate()
    {
        var validator = new SaleValidator();
        var result = validator.Validate(this);
        return new ValidationResultDetail
        {
            IsValid = result.IsValid,
            Errors = result.Errors.Select(o => (ValidationErrorDetail)o)
        };
    }

    private void RecalculateTotal()
        => TotalAmount = _items.Where(i => !i.IsCancelled).Sum(i => i.TotalAmount);

    private void Touch() => UpdatedAt = DateTime.UtcNow;

    private void RaiseEvent(DomainEvent evt) => _domainEvents.Add(evt);

    private void EnsureNotCancelled()
    {
        if (IsCancelled)
            throw new DomainException($"Sale {Id} is cancelled and cannot be modified.");
    }
}
