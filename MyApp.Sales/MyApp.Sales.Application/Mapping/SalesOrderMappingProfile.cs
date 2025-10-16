using AutoMapper;
using MyApp.Sales.Application.Contracts.DTOs;
using MyApp.Sales.Domain.Entities;

namespace MyApp.Sales.Application.Mapping
{
    public class SalesOrderMappingProfile : Profile
    {
        public SalesOrderMappingProfile()
        {
            CreateMap<SalesOrder, SalesOrderDto>()
                .ForMember(d => d.Status, opt => opt.MapFrom(s => (int)s.Status))
                .ReverseMap()
                .ForMember(d => d.Status, opt => opt.MapFrom(s => (SalesOrderStatus)s.Status));

            CreateMap<SalesOrderLine, SalesOrderLineDto>().ReverseMap();
            CreateMap<SalesOrderLine, CreateUpdateSalesOrderLineDto>().ReverseMap();

            CreateMap<Customer, CustomerDto>().ReverseMap();
            CreateMap<CreateUpdateSalesOrderDto, SalesOrder>()
                .ForMember(d => d.Status, opt => opt.MapFrom(s => (SalesOrderStatus)s.Status))
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.Lines, opt => opt.Ignore());
        }
    }
}
