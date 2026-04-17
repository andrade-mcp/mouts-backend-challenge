using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
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

public class CreateSaleHandlerTests
{
    private readonly ISaleRepository _repository = Substitute.For<ISaleRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly ILogger<CreateSaleHandler> _logger = Substitute.For<ILogger<CreateSaleHandler>>();
    private readonly CreateSaleHandler _handler;

    public CreateSaleHandlerTests()
    {
        _handler = new CreateSaleHandler(_repository, _mapper, _publisher, _logger);
        _mapper.Map<CreateSaleResult>(Arg.Any<Sale>())
            .Returns(ci => new CreateSaleResult { Id = ((Sale)ci.Args()[0]).Id });
    }

    [Fact(DisplayName = "Valid command persists the sale and publishes SaleCreatedEvent")]
    public async Task Handle_ValidCommand_PersistsAndPublishesEvent()
    {
        var command = SalesHandlerTestData.ValidCreateCommand();
        _repository.GetBySaleNumberAsync(command.SaleNumber, Arg.Any<CancellationToken>())
            .Returns((Sale?)null);
        _repository.CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(ci => (Sale)ci.Args()[0]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Id.Should().NotBeEmpty();
        await _repository.Received(1).CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        await _publisher.Received(1).Publish(Arg.Any<SaleCreatedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Duplicate sale number throws InvalidOperationException")]
    public async Task Handle_DuplicateSaleNumber_Throws()
    {
        var command = SalesHandlerTestData.ValidCreateCommand();
        _repository.GetBySaleNumberAsync(command.SaleNumber, Arg.Any<CancellationToken>())
            .Returns(SalesHandlerTestData.ValidSale(command.SaleNumber));

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*already exists*");
        await _repository.DidNotReceive().CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Empty command fails validation")]
    public async Task Handle_EmptyCommand_ThrowsValidationException()
    {
        var act = () => _handler.Handle(new CreateSaleCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }
}
