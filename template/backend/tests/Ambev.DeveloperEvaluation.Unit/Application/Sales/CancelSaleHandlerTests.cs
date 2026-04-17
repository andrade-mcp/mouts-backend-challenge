using Ambev.DeveloperEvaluation.Application.Sales.CancelSale;
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

public class CancelSaleHandlerTests
{
    private readonly ISaleRepository _repository = Substitute.For<ISaleRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly ILogger<CancelSaleHandler> _logger = Substitute.For<ILogger<CancelSaleHandler>>();
    private readonly CancelSaleHandler _handler;

    public CancelSaleHandlerTests()
    {
        _handler = new CancelSaleHandler(_repository, _mapper, _publisher, _logger);
        _mapper.Map<CancelSaleResult>(Arg.Any<Sale>())
            .Returns(ci => { var s = (Sale)ci.Args()[0]; return new CancelSaleResult { Id = s.Id, IsCancelled = s.IsCancelled }; });
    }

    [Fact(DisplayName = "Cancelling an active sale flips the flag and publishes SaleCancelledEvent")]
    public async Task Handle_ActiveSale_CancelsAndPublishes()
    {
        var sale = SalesHandlerTestData.ValidSale();
        _repository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        _repository.UpdateAsync(sale, Arg.Any<CancellationToken>()).Returns(sale);

        var result = await _handler.Handle(new CancelSaleCommand(sale.Id), CancellationToken.None);

        sale.IsCancelled.Should().BeTrue();
        result.IsCancelled.Should().BeTrue();
        await _publisher.Received(1).Publish(Arg.Any<SaleCancelledEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Missing sale throws KeyNotFoundException")]
    public async Task Handle_Missing_Throws()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Sale?)null);

        var act = () => _handler.Handle(new CancelSaleCommand(id), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact(DisplayName = "Cancelling an already-cancelled sale bubbles DomainException")]
    public async Task Handle_AlreadyCancelled_BubblesDomainException()
    {
        var sale = SalesHandlerTestData.ValidSale();
        sale.Cancel();
        _repository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);

        var act = () => _handler.Handle(new CancelSaleCommand(sale.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*already cancelled*");
        await _publisher.DidNotReceive().Publish(Arg.Any<SaleCancelledEvent>(), Arg.Any<CancellationToken>());
    }
}
