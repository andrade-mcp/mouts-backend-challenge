using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;

public class CancelSaleItemHandler : IRequestHandler<CancelSaleItemCommand, CancelSaleItemResult>
{
    private readonly ISaleRepository _repository;
    private readonly IPublisher _publisher;
    private readonly ILogger<CancelSaleItemHandler> _logger;

    public CancelSaleItemHandler(
        ISaleRepository repository,
        IPublisher publisher,
        ILogger<CancelSaleItemHandler> logger)
    {
        _repository = repository;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<CancelSaleItemResult> Handle(CancelSaleItemCommand command, CancellationToken cancellationToken)
    {
        var validation = await new CancelSaleItemValidator().ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var sale = await _repository.GetByIdAsync(command.SaleId, cancellationToken)
            ?? throw new KeyNotFoundException($"Sale {command.SaleId} not found.");

        sale.CancelItem(command.ItemId);   // DomainException if item missing or already cancelled

        await _repository.UpdateAsync(sale, cancellationToken);
        await PublishDomainEvents(sale, cancellationToken);

        return new CancelSaleItemResult
        {
            SaleId = sale.Id,
            ItemId = command.ItemId,
            SaleTotalAmount = sale.TotalAmount
        };
    }

    private async Task PublishDomainEvents(Sale sale, CancellationToken cancellationToken)
    {
        foreach (var evt in sale.DomainEvents)
        {
            try
            {
                await _publisher.Publish(evt, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish domain event {EventType} for sale {SaleId}",
                    evt.GetType().Name, sale.Id);
            }
        }
        sale.ClearDomainEvents();
    }
}
