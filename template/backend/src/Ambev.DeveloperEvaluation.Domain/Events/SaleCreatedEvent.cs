namespace Ambev.DeveloperEvaluation.Domain.Events;

public sealed record SaleCreatedEvent(Guid SaleId, string SaleNumber, decimal TotalAmount, DateTime OccurredAt)
    : DomainEvent(OccurredAt);
