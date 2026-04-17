using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Application.Sales.TestData;
using AutoMapper;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

public class UpdateSaleHandlerTests
{
    private readonly ISaleRepository _repository = Substitute.For<ISaleRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly ILogger<UpdateSaleHandler> _logger = Substitute.For<ILogger<UpdateSaleHandler>>();
    private readonly UpdateSaleHandler _handler;

    public UpdateSaleHandlerTests()
    {
        _handler = new UpdateSaleHandler(_repository, _mapper, _publisher, _logger);
        _mapper.Map<UpdateSaleResult>(Arg.Any<Sale>())
            .Returns(ci => new UpdateSaleResult { Id = ((Sale)ci.Args()[0]).Id });
    }

    [Fact(DisplayName = "Existing sale is mutated via ReplaceItems and SaleModifiedEvent is published")]
    public async Task Handle_Existing_PersistsAndPublishesEvent()
    {
        var sale = SalesHandlerTestData.ValidSale();
        _repository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        _repository.UpdateAsync(sale, Arg.Any<CancellationToken>()).Returns(sale);

        var command = SalesHandlerTestData.ValidUpdateCommand(sale.Id, itemCount: 2, quantity: 10, unitPrice: 5m);

        await _handler.Handle(command, CancellationToken.None);

        sale.Items.Should().HaveCount(2);
        sale.TotalAmount.Should().Be(2 * (10 * 5m * 0.8m));
        await _publisher.Received(1).Publish(Arg.Any<SaleModifiedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Missing sale throws KeyNotFoundException")]
    public async Task Handle_Missing_Throws()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Sale?)null);

        var act = () => _handler.Handle(SalesHandlerTestData.ValidUpdateCommand(id), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        await _publisher.DidNotReceive().Publish(Arg.Any<SaleModifiedEvent>(), Arg.Any<CancellationToken>());
    }
}
