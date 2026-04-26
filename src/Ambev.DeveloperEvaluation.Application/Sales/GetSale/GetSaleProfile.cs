using Ambev.DeveloperEvaluation.Domain.Entities;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.Application.Sales.GetSale;

public class GetSaleProfile : Profile
{
    public GetSaleProfile()
    {
        CreateMap<Customer, ExternalIdentityResult>()
            .ForMember(d => d.Id, o => o.MapFrom(c => c.Id))
            .ForMember(d => d.Name, o => o.MapFrom(c => c.Name));

        CreateMap<Branch, ExternalIdentityResult>()
            .ForMember(d => d.Id, o => o.MapFrom(b => b.Id))
            .ForMember(d => d.Name, o => o.MapFrom(b => b.Name));

        CreateMap<SaleItem, SaleItemResult>()
            .ForMember(d => d.ProductTitle, o => o.MapFrom(s => s.Product != null ? s.Product.Title : string.Empty));

        CreateMap<Sale, GetSaleResult>()
            .ForMember(d => d.Customer, o => o.MapFrom(s => s.Customer))
            .ForMember(d => d.Branch, o => o.MapFrom(s => s.Branch))
            .ForMember(d => d.CartId, o => o.MapFrom(s => s.CartId))
            .ForMember(d => d.Items, o => o.MapFrom(s => s.Items.OrderBy(i => i.Id)));
    }
}
