namespace Ambev.DeveloperEvaluation.Domain.Events;

public sealed record SaleCancelledEvent(Guid SaleId, string SaleNumber, DateTime OccurredAt)
    : DomainEvent(OccurredAt);
