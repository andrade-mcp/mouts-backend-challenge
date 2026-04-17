using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

public class CreateSaleHandler : IRequestHandler<CreateSaleCommand, CreateSaleResult>
{
    private readonly ISaleRepository _repository;
    private readonly IMapper _mapper;
    private readonly IPublisher _publisher;
    private readonly ILogger<CreateSaleHandler> _logger;

    public CreateSaleHandler(
        ISaleRepository repository,
        IMapper mapper,
        IPublisher publisher,
        ILogger<CreateSaleHandler> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<CreateSaleResult> Handle(CreateSaleCommand command, CancellationToken cancellationToken)
    {
        var validator = new CreateSaleValidator();
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var duplicate = await _repository.GetBySaleNumberAsync(command.SaleNumber, cancellationToken);
        if (duplicate is not null)
            throw new InvalidOperationException($"Sale with number '{command.SaleNumber}' already exists.");

        var sale = new Sale
        {
            SaleNumber = command.SaleNumber,
            SaleDate = command.SaleDate,
            CustomerId = command.CustomerId,
            CustomerName = command.CustomerName,
            BranchId = command.BranchId,
            BranchName = command.BranchName,
        };

        foreach (var i in command.Items)
            sale.AddItem(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice);

        sale.MarkCreated();

        await _repository.CreateAsync(sale, cancellationToken);
        await PublishDomainEvents(sale, cancellationToken);

        return _mapper.Map<CreateSaleResult>(sale);
    }

    /// <summary>
    /// Publishes after the write succeeds so a failed save doesn't emit phantom
    /// events. Publish failures are logged and swallowed — a broken log sink
    /// must not undo a committed write.
    /// </summary>
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
