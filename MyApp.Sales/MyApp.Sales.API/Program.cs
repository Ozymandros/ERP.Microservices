using Microsoft.EntityFrameworkCore;
using MyApp.Sales.Domain;
using MyApp.Sales.Infrastructure.Data;
using MyApp.Shared.Infrastructure.Extensions;

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
var salesDbConnectionString = builder.Configuration.GetConnectionString("salesdb");
builder.Services.AddDbContext<SalesDbContext>(options =>
    options.UseSqlServer(salesDbConnectionString));

builder.Services.AddScoped<MyApp.Sales.Domain.ISalesOrderLineRepository, MyApp.Sales.Infrastructure.Data.Repositories.SalesOrderLineRepository>();
builder.Services.AddScoped<MyApp.Sales.Domain.ISalesOrderRepository, MyApp.Sales.Infrastructure.Data.Repositories.SalesOrderRepository>();
builder.Services.AddScoped<MyApp.Sales.Domain.ICustomerRepository, MyApp.Sales.Infrastructure.Data.Repositories.CustomerRepository>();

builder.Services.AddAutoMapper(
    cfg => { /* optional configuration */ },
    typeof(MyApp.Sales.Application.Mapping.SalesOrderMappingProfile).Assembly
);
builder.Services.AddScoped<MyApp.Sales.Application.Contracts.Services.ISalesOrderService, MyApp.Sales.Application.Services.SalesOrderService>();

builder.Services.AddScoped<IPermissionChecker, DaprPermissionChecker>();

var app = builder.Build();

// Afegeix un bloc per a l'aplicaci� autom�tica de les migracions
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
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
