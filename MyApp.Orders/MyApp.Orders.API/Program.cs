using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Orders.Infrastructure.Data;
using MyApp.Shared.Infrastructure.Extensions;
using System.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Aquesta línia registra el DaprClient (Singleton) al contenidor d'Injecció de Dependències (DI)
builder.Services.AddDaprClient();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Infrastructure & Application DI
var ordersDbConnectionString = builder.Configuration.GetConnectionString("ordersdb");
builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseSqlServer(ordersDbConnectionString));

builder.Services.AddScoped<MyApp.Orders.Domain.IOrderRepository, MyApp.Orders.Infrastructure.Repositories.OrderRepository>();
builder.Services.AddScoped<MyApp.Orders.Domain.IOrderLineRepository, MyApp.Orders.Infrastructure.Repositories.OrderLineRepository>();

builder.Services.AddAutoMapper(
    cfg => { /* optional configuration */ },
    typeof(MyApp.Orders.Application.Mapping.OrderProfile).Assembly
);
builder.Services.AddScoped<MyApp.Orders.Application.Contracts.IOrderService, MyApp.Orders.Application.Services.OrderService>();

builder.Services.AddScoped<IPermissionChecker, DaprPermissionChecker>();

var app = builder.Build();

// Afegeix un bloc per a l'aplicaci� autom�tica de les migracions
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
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

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
