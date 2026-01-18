using AutoMapper;
using MyApp.Purchasing.Application.Contracts.DTOs;
using MyApp.Purchasing.Domain.Entities;

namespace MyApp.Purchasing.Application.Mappings;

public class PurchasingMappingProfile : Profile
{
    public PurchasingMappingProfile()
    {
        // Supplier mappings
        CreateMap<Supplier, SupplierDto>();
        CreateMap<SupplierDto, Supplier>()
            .ConstructUsing(src => new Supplier(src.Id));
        CreateMap<CreateUpdateSupplierDto, Supplier>()
            .ConstructUsing(src => new Supplier(Guid.NewGuid()));

        // PurchaseOrder mappings
        CreateMap<PurchaseOrder, PurchaseOrderDto>()
            .ForMember(dest => dest.Supplier, opt => opt.MapFrom(src => src.Supplier))
            .ForMember(dest => dest.Lines, opt => opt.MapFrom(src => src.Lines));
        
        CreateMap<CreateUpdatePurchaseOrderDto, PurchaseOrder>()
            .ConstructUsing(src => new PurchaseOrder(Guid.NewGuid()))
            .ForMember(dest => dest.Lines, opt => opt.MapFrom(src => src.Lines));

        // PurchaseOrderLine mappings (PurchaseOrderLine doesn't have primary constructor, ReverseMap is fine)
        CreateMap<PurchaseOrderLine, PurchaseOrderLineDto>().ReverseMap();
        CreateMap<CreateUpdatePurchaseOrderLineDto, PurchaseOrderLine>();
    }
}
