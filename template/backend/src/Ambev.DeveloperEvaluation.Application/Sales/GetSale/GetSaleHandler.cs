using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.GetSale;

public class GetSaleHandler : IRequestHandler<GetSaleQuery, GetSaleResult>
{
    private readonly ISaleRepository _repository;
    private readonly IMapper _mapper;

    public GetSaleHandler(ISaleRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<GetSaleResult> Handle(GetSaleQuery query, CancellationToken cancellationToken)
    {
        var validation = await new GetSaleValidator().ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var sale = await _repository.GetByIdAsync(query.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Sale {query.Id} not found.");

        return _mapper.Map<GetSaleResult>(sale);
    }
}
