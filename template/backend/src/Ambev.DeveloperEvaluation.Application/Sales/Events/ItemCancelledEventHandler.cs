using Ambev.DeveloperEvaluation.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales.Events;

public class ItemCancelledEventHandler : INotificationHandler<ItemCancelledEvent>
{
    private readonly ILogger<ItemCancelledEventHandler> _logger;

    public ItemCancelledEventHandler(ILogger<ItemCancelledEventHandler> logger) => _logger = logger;

    public Task Handle(ItemCancelledEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Sale item cancelled: sale={SaleId} item={ItemId} product={ProductId} qty={Quantity} at {OccurredAt:o}",
            notification.SaleId, notification.ItemId, notification.ProductId,
            notification.Quantity, notification.OccurredAt);
        return Task.CompletedTask;
    }
}
