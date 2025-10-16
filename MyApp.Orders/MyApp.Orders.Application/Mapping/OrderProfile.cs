using AutoMapper;
using MyApp.Orders.Application.Contracts.Dtos;
using MyApp.Orders.Domain.Entities;

namespace MyApp.Orders.Application.Mapping
{
    public class OrderProfile : Profile
    {
        public OrderProfile()
        {
            CreateMap<Order, OrderDto>()
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));

            CreateMap<OrderLine, OrderLineDto>();

            CreateMap<CreateUpdateOrderDto, Order>()
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.Lines, o => o.MapFrom(s => s.Lines));

            CreateMap<OrderLineDto, OrderLine>();
        }
    }
}
