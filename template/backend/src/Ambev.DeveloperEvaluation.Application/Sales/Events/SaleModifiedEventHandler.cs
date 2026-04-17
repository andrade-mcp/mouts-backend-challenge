using Ambev.DeveloperEvaluation.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales.Events;

public class SaleModifiedEventHandler : INotificationHandler<SaleModifiedEvent>
{
    private readonly ILogger<SaleModifiedEventHandler> _logger;

    public SaleModifiedEventHandler(ILogger<SaleModifiedEventHandler> logger) => _logger = logger;

    public Task Handle(SaleModifiedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Sale modified: {SaleId} ({SaleNumber}) total={TotalAmount} at {OccurredAt:o}",
            notification.SaleId, notification.SaleNumber, notification.TotalAmount, notification.OccurredAt);
        return Task.CompletedTask;
    }
}
