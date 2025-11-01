using AutoMapper;
using MyApp.Purchasing.Application.Contracts.DTOs;
using MyApp.Purchasing.Domain.Entities;

namespace MyApp.Purchasing.Application.Mappings;

public class PurchasingMappingProfile : Profile
{
    public PurchasingMappingProfile()
    {
        // Supplier mappings
        CreateMap<Supplier, SupplierDto>().ReverseMap();
        CreateMap<CreateUpdateSupplierDto, Supplier>();

        // PurchaseOrder mappings
        CreateMap<PurchaseOrder, PurchaseOrderDto>()
            .ForMember(dest => dest.Supplier, opt => opt.MapFrom(src => src.Supplier))
            .ForMember(dest => dest.Lines, opt => opt.MapFrom(src => src.Lines));
        
        CreateMap<CreateUpdatePurchaseOrderDto, PurchaseOrder>()
            .ForMember(dest => dest.Lines, opt => opt.MapFrom(src => src.Lines));

        // PurchaseOrderLine mappings
        CreateMap<PurchaseOrderLine, PurchaseOrderLineDto>().ReverseMap();
        CreateMap<CreateUpdatePurchaseOrderLineDto, PurchaseOrderLine>();
    }
}
