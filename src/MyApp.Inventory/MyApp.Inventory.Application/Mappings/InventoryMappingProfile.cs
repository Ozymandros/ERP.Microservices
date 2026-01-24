using AutoMapper;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Domain.Entities;

namespace MyApp.Inventory.Application.Mappings;

public class InventoryMappingProfile : Profile
{
    public InventoryMappingProfile()
    {
        // Product mappings
        CreateMap<Product, ProductDto>();
        CreateMap<ProductDto, Product>()
            .ConstructUsing(src => new Product(src.Id));
        
        // CreateUpdateProductDto -> Product mapping with explicit construction
        CreateMap<CreateUpdateProductDto, Product>()
            .ConstructUsing(src => new Product(Guid.NewGuid()));

        // Warehouse mappings
        CreateMap<Warehouse, WarehouseDto>();
        CreateMap<WarehouseDto, Warehouse>()
            .ConstructUsing(src => new Warehouse(src.Id));
        CreateMap<CreateUpdateWarehouseDto, Warehouse>()
            .ConstructUsing(src => new Warehouse(Guid.NewGuid()));

        // InventoryTransaction mappings
        CreateMap<InventoryTransaction, InventoryTransactionDto>()
            .ForMember(dest => dest.Product, opt => opt.MapFrom(src => src.Product))
            .ForMember(dest => dest.Warehouse, opt => opt.MapFrom(src => src.Warehouse));

        CreateMap<CreateUpdateInventoryTransactionDto, InventoryTransaction>()
            .ConstructUsing(src => new InventoryTransaction(Guid.NewGuid()));
        
        // WarehouseStock mappings
        CreateMap<WarehouseStock, WarehouseStockDto>()
            .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Warehouse != null ? src.Warehouse.Name : null));
    }
}
