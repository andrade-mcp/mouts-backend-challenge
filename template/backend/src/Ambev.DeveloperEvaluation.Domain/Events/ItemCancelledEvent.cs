namespace Ambev.DeveloperEvaluation.Domain.Events;

public sealed record ItemCancelledEvent(Guid SaleId, Guid ItemId, Guid ProductId, int Quantity, DateTime OccurredAt)
    : DomainEvent(OccurredAt);
