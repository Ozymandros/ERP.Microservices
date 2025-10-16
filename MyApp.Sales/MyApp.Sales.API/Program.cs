using Microsoft.EntityFrameworkCore;
using MyApp.Sales.Application.Mapping;
using MyApp.Sales.Application.Services;
using MyApp.Sales.Application.Contracts.Services;
using MyApp.Sales.Infrastructure.Data;
using MyApp.Sales.Infrastructure.Data.Repositories;
using MyApp.Sales.Domain;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add DbContext
builder.Services.AddDbContext<SalesDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Server=(localdb)\\mssqllocaldb;Database=SalesDb;Trusted_Connection=true;"));

// Add Repositories
builder.Services.AddScoped<ISalesOrderRepository, SalesOrderRepository>();
builder.Services.AddScoped<ISalesOrderLineRepository, SalesOrderLineRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();

// Add Application Services
builder.Services.AddScoped<ISalesOrderService, SalesOrderService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(SalesOrderMappingProfile));

// Add controllers
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
