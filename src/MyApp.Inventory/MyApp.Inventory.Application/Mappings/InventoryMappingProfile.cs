using AutoMapper;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Domain.Entities;

namespace MyApp.Inventory.Application.Mappings;

public class InventoryMappingProfile : Profile
{
    public InventoryMappingProfile()
    {
        // #region agent log
        try { System.IO.File.AppendAllText(@"c:\Projects\ERP_ASPIRE_APP\erp-backend\.cursor\debug.log", $"{{\"sessionId\":\"debug-session\",\"runId\":\"post-fix\",\"hypothesisId\":\"C\",\"location\":\"InventoryMappingProfile.cs:11\",\"message\":\"Mapping profile constructor called\",\"data\":{{}},\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n"); } catch { }
        // #endregion

        // Product mappings
        CreateMap<Product, ProductDto>();
        CreateMap<ProductDto, Product>()
            .ConstructUsing(src => new Product(src.Id));
        
        // CreateUpdateProductDto -> Product mapping with explicit construction
        // #region agent log
        try { System.IO.File.AppendAllText(@"c:\Projects\ERP_ASPIRE_APP\erp-backend\.cursor\debug.log", $"{{\"sessionId\":\"debug-session\",\"runId\":\"post-fix\",\"hypothesisId\":\"C\",\"location\":\"InventoryMappingProfile.cs:17\",\"message\":\"Creating CreateUpdateProductDto to Product mapping\",\"data\":{{}},\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n"); } catch { }
        // #endregion
        
        CreateMap<CreateUpdateProductDto, Product>()
            .ConstructUsing(src => new Product(Guid.NewGuid()));

        // Warehouse mappings
        CreateMap<Warehouse, WarehouseDto>().ReverseMap();
        CreateMap<CreateUpdateWarehouseDto, Warehouse>()
            .ConstructUsing(src => new Warehouse(Guid.NewGuid()));

        // InventoryTransaction mappings
        CreateMap<InventoryTransaction, InventoryTransactionDto>()
            .ForMember(dest => dest.Product, opt => opt.MapFrom(src => src.Product))
            .ForMember(dest => dest.Warehouse, opt => opt.MapFrom(src => src.Warehouse));

        CreateMap<CreateUpdateInventoryTransactionDto, InventoryTransaction>()
            .ConstructUsing(src => new InventoryTransaction(Guid.NewGuid()));
        
        // WarehouseStock mappings
        CreateMap<WarehouseStock, WarehouseStockDto>();
    }
}
