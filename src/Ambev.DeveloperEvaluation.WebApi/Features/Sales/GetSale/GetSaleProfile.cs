using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.GetSale;

public class GetSaleProfile : Profile
{
    public GetSaleProfile()
    {
        CreateMap<ExternalIdentityResult, ExternalIdentityResponse>();
        CreateMap<SaleItemResult, SaleItemResponse>();
        CreateMap<GetSaleResult, GetSaleResponse>();
        CreateMap<int, GetSaleCommand>()
            .ConstructUsing(id => new GetSaleCommand(id));
    }
}

