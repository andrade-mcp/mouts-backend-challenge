namespace Ambev.DeveloperEvaluation.Domain.Events;

public sealed record SaleModifiedEvent(Guid SaleId, string SaleNumber, decimal TotalAmount, DateTime OccurredAt)
    : DomainEvent(OccurredAt);
