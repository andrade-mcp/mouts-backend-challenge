using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

public class ListSalesHandler : IRequestHandler<ListSalesQuery, ListSalesResult>
{
    private readonly ISaleRepository _repository;
    private readonly IMapper _mapper;

    public ListSalesHandler(ISaleRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<ListSalesResult> Handle(ListSalesQuery query, CancellationToken cancellationToken)
    {
        var validation = await new ListSalesValidator().ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var (items, total) = await _repository.ListAsync(
            query.Page,
            query.PageSize,
            query.SaleNumber,
            query.CustomerId,
            query.BranchId,
            query.StartDate,
            query.EndDate,
            query.IncludeCancelled,
            query.OrderBy,
            cancellationToken);

        return new ListSalesResult
        {
            Items = _mapper.Map<IReadOnlyList<SaleSummaryResult>>(items),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total
        };
    }
}
