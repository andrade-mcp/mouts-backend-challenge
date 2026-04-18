using Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Application.Sales.TestData;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

public class CancelSaleItemHandlerTests
{
    private readonly ISaleRepository _repository = Substitute.For<ISaleRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly ILogger<CancelSaleItemHandler> _logger = Substitute.For<ILogger<CancelSaleItemHandler>>();
    private readonly CancelSaleItemHandler _handler;

    public CancelSaleItemHandlerTests()
    {
        _handler = new CancelSaleItemHandler(_repository, _publisher, _logger);
    }

    [Fact(DisplayName = "Cancelling an existing item excludes it from total and publishes ItemCancelledEvent")]
    public async Task Handle_ExistingItem_CancelsAndPublishes()
    {
        var sale = SalesHandlerTestData.ValidSale(itemQuantity: 2, unitPrice: 10m);   // 20
        var extra = sale.AddItem(Guid.NewGuid(), "X", 5, 10m);                         // +45 → 65
        _repository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        _repository.UpdateAsync(sale, Arg.Any<CancellationToken>()).Returns(sale);

        var result = await _handler.Handle(new CancelSaleItemCommand(sale.Id, extra.Id), CancellationToken.None);

        result.SaleTotalAmount.Should().Be(20m);
        result.ItemId.Should().Be(extra.Id);
        await _publisher.Received(1).Publish(Arg.Any<ItemCancelledEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Missing sale throws KeyNotFoundException")]
    public async Task Handle_MissingSale_Throws()
    {
        var saleId = Guid.NewGuid();
        _repository.GetByIdAsync(saleId, Arg.Any<CancellationToken>()).Returns((Sale?)null);

        var act = () => _handler.Handle(new CancelSaleItemCommand(saleId, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact(DisplayName = "Unknown item id bubbles DomainException from the aggregate")]
    public async Task Handle_UnknownItem_BubblesDomainException()
    {
        var sale = SalesHandlerTestData.ValidSale();
        _repository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);

        var act = () => _handler.Handle(new CancelSaleItemCommand(sale.Id, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*not found*");
        await _publisher.DidNotReceive().Publish(Arg.Any<ItemCancelledEvent>(), Arg.Any<CancellationToken>());
    }
}
