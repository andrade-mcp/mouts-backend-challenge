using Ambev.DeveloperEvaluation.Domain.Entities;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

public class ListSalesProfile : Profile
{
    public ListSalesProfile()
    {
        CreateMap<Sale, SaleSummaryResult>()
            .ForMember(d => d.ItemCount, opt => opt.MapFrom(s => s.Items.Count));
    }
}
