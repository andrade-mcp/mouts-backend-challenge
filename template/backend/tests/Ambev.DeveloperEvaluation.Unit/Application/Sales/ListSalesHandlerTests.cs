using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Application.Sales.TestData;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

public class ListSalesHandlerTests
{
    private readonly ISaleRepository _repository = Substitute.For<ISaleRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly ListSalesHandler _handler;

    public ListSalesHandlerTests()
    {
        _handler = new ListSalesHandler(_repository, _mapper);
    }

    [Fact(DisplayName = "Happy path returns the page plus pagination metadata")]
    public async Task Handle_Valid_ReturnsPagedResult()
    {
        var query = SalesHandlerTestData.ValidListQuery(page: 2, pageSize: 5);
        var sales = new[] { SalesHandlerTestData.ValidSale(), SalesHandlerTestData.ValidSale() };
        _repository.ListAsync(
            query.Page, query.PageSize, query.SaleNumber, query.CustomerId, query.BranchId,
            query.StartDate, query.EndDate, query.IncludeCancelled, query.OrderBy,
            Arg.Any<CancellationToken>())
          .Returns((sales, 12));
        _mapper.Map<IReadOnlyList<SaleSummaryResult>>(Arg.Any<IReadOnlyList<Sale>>())
               .Returns(new List<SaleSummaryResult> { new(), new() });

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(5);
        result.TotalCount.Should().Be(12);
        result.TotalPages.Should().Be(3);  // ceil(12 / 5)
    }

    [Fact(DisplayName = "Inverted date range fails validation")]
    public async Task Handle_InvertedDateRange_ThrowsValidationException()
    {
        var query = SalesHandlerTestData.ValidListQuery();
        query.StartDate = new DateTime(2026, 5, 1);
        query.EndDate = new DateTime(2026, 4, 1);

        var act = () => _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>()
               .WithMessage("*StartDate*EndDate*");
    }

    [Fact(DisplayName = "PageSize above the cap fails validation")]
    public async Task Handle_OversizedPage_ThrowsValidationException()
    {
        var query = SalesHandlerTestData.ValidListQuery(pageSize: ListSalesValidator.MaxPageSize + 1);

        var act = () => _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }
}
