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
                .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status));
            CreateMap<SalesOrderDto, SalesOrder>()
                .ConstructUsing(src => new SalesOrder(src.Id))
                .ForMember(d => d.Status, opt => opt.MapFrom(s => (SalesOrderStatus)s.Status));

            CreateMap<SalesOrderLine, SalesOrderLineDto>();
            CreateMap<SalesOrderLineDto, SalesOrderLine>()
                .ConstructUsing(src => new SalesOrderLine(src.Id));
            CreateMap<SalesOrderLine, CreateUpdateSalesOrderLineDto>();
            CreateMap<CreateUpdateSalesOrderLineDto, SalesOrderLine>()
                .ConstructUsing(src => new SalesOrderLine(Guid.NewGuid()));

            CreateMap<Customer, CustomerDto>();
            CreateMap<CustomerDto, Customer>()
                .ConstructUsing(src => new Customer(src.Id));
            CreateMap<CreateUpdateSalesOrderDto, SalesOrder>()
                .ConstructUsing(src => new SalesOrder(Guid.NewGuid()))
                .ForMember(d => d.Status, opt => opt.MapFrom(s => (SalesOrderStatus)s.Status))
                .ForMember(d => d.Lines, opt => opt.Ignore());
        }
    }
}
