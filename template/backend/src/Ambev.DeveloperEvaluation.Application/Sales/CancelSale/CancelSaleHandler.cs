using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales.CancelSale;

public class CancelSaleHandler : IRequestHandler<CancelSaleCommand, CancelSaleResult>
{
    private readonly ISaleRepository _repository;
    private readonly IMapper _mapper;
    private readonly IPublisher _publisher;
    private readonly ILogger<CancelSaleHandler> _logger;

    public CancelSaleHandler(
        ISaleRepository repository,
        IMapper mapper,
        IPublisher publisher,
        ILogger<CancelSaleHandler> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<CancelSaleResult> Handle(CancelSaleCommand command, CancellationToken cancellationToken)
    {
        var validation = await new CancelSaleValidator().ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var sale = await _repository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Sale {command.Id} not found.");

        sale.Cancel();   // DomainException if already cancelled → maps to 409 at the API

        await _repository.UpdateAsync(sale, cancellationToken);
        await PublishDomainEvents(sale, cancellationToken);

        return _mapper.Map<CancelSaleResult>(sale);
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
