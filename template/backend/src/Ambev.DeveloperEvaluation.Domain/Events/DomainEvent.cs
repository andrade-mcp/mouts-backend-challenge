using MediatR;

namespace Ambev.DeveloperEvaluation.Domain.Events;

/// <summary>Base for domain events, dispatched via MediatR after a write succeeds.</summary>
public abstract record DomainEvent(DateTime OccurredAt) : INotification;
