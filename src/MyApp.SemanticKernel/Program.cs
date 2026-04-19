using MyApp.Shared.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults(new("MyApp.SemanticKernel.API"));

var app = builder.Build();

app.UseServiceDefaults();

app.Run();
