using AutoMapper;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Domain.Entities;

namespace MyApp.Inventory.Application.Mappings;

public class InventoryMappingProfile : Profile
{
    public InventoryMappingProfile()
    {
        // Product mappings
        CreateMap<Product, ProductDto>().ReverseMap();
        CreateMap<CreateUpdateProductDto, Product>();

        // Warehouse mappings
        CreateMap<Warehouse, WarehouseDto>().ReverseMap();
        CreateMap<CreateUpdateWarehouseDto, Warehouse>();

        // InventoryTransaction mappings
        CreateMap<InventoryTransaction, InventoryTransactionDto>()
            .ForMember(dest => dest.Product, opt => opt.MapFrom(src => src.Product))
            .ForMember(dest => dest.Warehouse, opt => opt.MapFrom(src => src.Warehouse));

        CreateMap<CreateUpdateInventoryTransactionDto, InventoryTransaction>();
        
        // WarehouseStock mappings
        CreateMap<WarehouseStock, WarehouseStockDto>();
    }
}
