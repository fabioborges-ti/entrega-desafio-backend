using Ambev.DeveloperEvaluation.Domain.Entities;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.Application.Carts;

public class CartProfile : Profile
{
    public CartProfile()
    {
        CreateMap<CartLineItem, CartProductDto>();
        CreateMap<Cart, CartDto>()
            .ForMember(d => d.Date, o => o.MapFrom(s => s.Date.ToString("yyyy-MM-dd")))
            .ForMember(d => d.Products, o => o.MapFrom(s => s.LineItems.OrderBy(li => li.Id)));
    }
}
