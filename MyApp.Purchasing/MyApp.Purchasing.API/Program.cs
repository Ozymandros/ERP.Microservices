using Microsoft.EntityFrameworkCore;
using MyApp.Purchasing.Application.Contracts.Services;
using MyApp.Purchasing.Application.Services;
using MyApp.Purchasing.Domain.Repositories;
using MyApp.Purchasing.Infrastructure.Data;
using MyApp.Purchasing.Infrastructure.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database configuration
var connectionString = builder.Configuration.GetConnectionString("PurchasingDb") 
    ?? "Server=localhost;Database=PurchasingDb;Trusted_Connection=True;";
builder.Services.AddDbContext<PurchasingDbContext>(options =>
    options.UseSqlServer(connectionString));

// Repository registration
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
builder.Services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
builder.Services.AddScoped<IPurchaseOrderLineRepository, PurchaseOrderLineRepository>();

// Service registration
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();

// AutoMapper registration
builder.Services.AddAutoMapper(
    cfg => { /* optional configuration */ },
    typeof(MyApp.Purchasing.Application.Mappings.PurchasingMappingProfile).Assembly
);

var app = builder.Build();

// Afegeix un bloc per a l'aplicaci� autom�tica de les migracions
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PurchasingDbContext>();
    // ATENCI�: Esborra la base de dades i torna-la a crear si est� buida (�til per a desenvolupament amb contenidors)
    // dbContext.Database.EnsureDeleted(); 

    // Aquest �s el m�tode clau: aplica les migracions pendents.
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
