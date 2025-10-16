using Microsoft.EntityFrameworkCore;
using MyApp.Inventory.Infrastructure.Data;
using MyApp.Shared.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Infrastructure & Application DI
var inventoryDbConnectionString = builder.Configuration.GetConnectionString("inventorydb");
builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseSqlServer(inventoryDbConnectionString));

builder.Services.AddScoped<MyApp.Inventory.Domain.Repositories.IInventoryTransactionRepository, MyApp.Inventory.Infrastructure.Data.Repositories.InventoryTransactionRepository>();
builder.Services.AddScoped<MyApp.Inventory.Domain.Repositories.IProductRepository, MyApp.Inventory.Infrastructure.Data.Repositories.ProductRepository>();
builder.Services.AddScoped<MyApp.Inventory.Domain.Repositories.IWarehouseRepository, MyApp.Inventory.Infrastructure.Data.Repositories.WarehouseRepository>();

builder.Services.AddAutoMapper(
    cfg => { /* optional configuration */ },
    typeof(MyApp.Inventory.Application.Mappings.InventoryMappingProfile).Assembly
);
builder.Services.AddScoped<MyApp.Inventory.Application.Contracts.Services.IInventoryTransactionService, MyApp.Inventory.Application.Services.InventoryTransactionService>();
builder.Services.AddScoped<MyApp.Inventory.Application.Contracts.Services.IProductService, MyApp.Inventory.Application.Services.ProductService>();
builder.Services.AddScoped<MyApp.Inventory.Application.Contracts.Services.IWarehouseService, MyApp.Inventory.Application.Services.WarehouseService>();

var app = builder.Build();

// Afegeix un bloc per a l'aplicaci� autom�tica de les migracions
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    // ATENCI�: Esborra la base de dades i torna-la a crear si est� buida (�til per a desenvolupament amb contenidors)
    // dbContext.Database.EnsureDeleted(); 

    // Aquest �s el m�tode clau: aplica les migracions pendents.
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
