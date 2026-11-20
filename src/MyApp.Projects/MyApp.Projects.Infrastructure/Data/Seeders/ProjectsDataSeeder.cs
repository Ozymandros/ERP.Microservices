using Microsoft.EntityFrameworkCore;
using MyApp.Projects.Domain.Entities;

namespace MyApp.Projects.Infrastructure.Data.Seeders;

/// <summary>
/// Seeds reference data (currencies, demo exchange rates, resource groups, resources) when the tables are empty.
/// Demo exchange rates are only seeded when the local currency is EUR.
/// </summary>
public static class ProjectsDataSeeder
{
    /// <summary>Seeds reference data if missing.</summary>
    /// <param name="context">The context.</param>
    /// <param name="localCurrency">The local currency code (LCY).</param>
    public static async Task SeedAsync(ProjectsDbContext context, string localCurrency)
    {
        var lcy = localCurrency.ToUpperInvariant();

        if (!await context.Currencies.AnyAsync())
        {
            var currencies = new[]
            {
                (Code: "EUR", Description: "Euro", Symbol: "€"),
                (Code: "USD", Description: "US Dollar", Symbol: "$"),
                (Code: "GBP", Description: "Pound Sterling", Symbol: "£")
            };

            foreach (var c in currencies)
            {
                context.Currencies.Add(new Currency(Guid.NewGuid())
                {
                    Code = c.Code,
                    Description = c.Description,
                    Symbol = c.Symbol
                });
            }

            if (!currencies.Any(c => c.Code == lcy))
            {
                context.Currencies.Add(new Currency(Guid.NewGuid()) { Code = lcy, Description = lcy });
            }

            await context.SaveChangesAsync();

            if (lcy == "EUR")
            {
                // Demo rates only (LCY per 1 unit). Maintain real rates through the API.
                var start = new DateTime(DateTime.UtcNow.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var usd = await context.Currencies.FirstAsync(c => c.Code == "USD");
                var gbp = await context.Currencies.FirstAsync(c => c.Code == "GBP");
                context.ExchangeRates.Add(new ExchangeRate(Guid.NewGuid()) { CurrencyId = usd.Id, StartingDate = start, RateToLocal = 0.92m });
                context.ExchangeRates.Add(new ExchangeRate(Guid.NewGuid()) { CurrencyId = gbp.Id, StartingDate = start, RateToLocal = 1.17m });
                await context.SaveChangesAsync();
            }
        }

        if (!await context.ResourceGroups.AnyAsync())
        {
            var consultants = new ResourceGroup(Guid.NewGuid()) { Code = "CONSULT", Name = "Consultants" };
            var machinery = new ResourceGroup(Guid.NewGuid()) { Code = "MACHINE", Name = "Machinery" };
            context.ResourceGroups.AddRange(consultants, machinery);

            if (!await context.Resources.AnyAsync())
            {
                context.Resources.AddRange(
                    new Resource(Guid.NewGuid())
                    {
                        ResourceNo = "R0001", Name = "Senior Consultant", Type = ResourceType.Person,
                        ResourceGroupId = consultants.Id, BaseUnitOfMeasure = "HOUR", UnitCost = 45m, UnitPrice = 90m
                    },
                    new Resource(Guid.NewGuid())
                    {
                        ResourceNo = "R0002", Name = "Junior Consultant", Type = ResourceType.Person,
                        ResourceGroupId = consultants.Id, BaseUnitOfMeasure = "HOUR", UnitCost = 25m, UnitPrice = 55m
                    },
                    new Resource(Guid.NewGuid())
                    {
                        ResourceNo = "M0001", Name = "Forklift", Type = ResourceType.Machine,
                        ResourceGroupId = machinery.Id, BaseUnitOfMeasure = "HOUR", UnitCost = 15m, UnitPrice = 35m,
                        DailyCapacity = 10m
                    });
            }

            await context.SaveChangesAsync();
        }
    }
}
