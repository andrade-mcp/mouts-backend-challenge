using Ambev.DeveloperEvaluation.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales.Events;

/// <summary>
/// Log handler for <see cref="SaleCreatedEvent"/>
/// </summary>
public class SaleCreatedEventHandler : INotificationHandler<SaleCreatedEvent>
{
    private readonly ILogger<SaleCreatedEventHandler> _logger;

    public SaleCreatedEventHandler(ILogger<SaleCreatedEventHandler> logger) => _logger = logger;

    public Task Handle(SaleCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Sale created: {SaleId} ({SaleNumber}) total={TotalAmount} at {OccurredAt:o}",
            notification.SaleId, notification.SaleNumber, notification.TotalAmount, notification.OccurredAt);
        return Task.CompletedTask;
    }
}
