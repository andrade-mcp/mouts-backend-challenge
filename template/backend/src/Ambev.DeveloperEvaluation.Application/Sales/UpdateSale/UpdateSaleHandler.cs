using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;

public class UpdateSaleHandler : IRequestHandler<UpdateSaleCommand, UpdateSaleResult>
{
    private readonly ISaleRepository _repository;
    private readonly IMapper _mapper;
    private readonly IPublisher _publisher;
    private readonly ILogger<UpdateSaleHandler> _logger;

    public UpdateSaleHandler(
        ISaleRepository repository,
        IMapper mapper,
        IPublisher publisher,
        ILogger<UpdateSaleHandler> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<UpdateSaleResult> Handle(UpdateSaleCommand command, CancellationToken cancellationToken)
    {
        var validation = await new UpdateSaleValidator().ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var sale = await _repository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Sale {command.Id} not found.");

        sale.SaleDate = command.SaleDate;
        sale.CustomerId = command.CustomerId;
        sale.CustomerName = command.CustomerName;
        sale.BranchId = command.BranchId;
        sale.BranchName = command.BranchName;

        sale.ReplaceItems(command.Items.Select(i =>
            (i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)));

        await _repository.UpdateAsync(sale, cancellationToken);
        await PublishDomainEvents(sale, cancellationToken);

        return _mapper.Map<UpdateSaleResult>(sale);
    }

    private async Task PublishDomainEvents(Domain.Entities.Sale sale, CancellationToken cancellationToken)
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
