using Ambev.DeveloperEvaluation.Domain.Entities;

using AutoMapper;



namespace Ambev.DeveloperEvaluation.Application.Inventories;



public class InventoryProfile : Profile

{

    public InventoryProfile()

    {

        CreateMap<Inventory, InventoryDto>()
            .ForMember(d => d.ProductTitle, o => o.MapFrom(s => s.Product != null ? s.Product.Title : string.Empty))
            .ForMember(d => d.CategoryId, o => o.MapFrom(s => s.Product != null ? s.Product.CategoryId : 0))
            .ForMember(d => d.Category, o => o.MapFrom(s =>
                s.Product != null && s.Product.Category != null ? s.Product.Category.Name : string.Empty))
            .ForMember(d => d.IsBelowAlert, o => o.MapFrom(s => s.IsStockBelowAlert()));

    }

}


