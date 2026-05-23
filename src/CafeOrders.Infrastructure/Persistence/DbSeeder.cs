using CafeOrders.Domain.Entities;
using CafeOrders.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CafeOrders.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(CafeOrdersDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await EnsureDefaultTablesAsync(dbContext, cancellationToken);

        if (!dbContext.Categories.Any())
        {
            var categories = new[]
            {
                new Category { Name = "Su ve Soda", SortOrder = 1 },
                new Category { Name = "Gazli Icecekler", SortOrder = 2 },
                new Category { Name = "FuseTea", SortOrder = 3 },
                new Category { Name = "Enerji Icecekleri", SortOrder = 4 }
            };

            await dbContext.Categories.AddRangeAsync(categories, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            var waterAndSodaId = dbContext.Categories.Single(x => x.Name == "Su ve Soda").Id;
            var carbonatedId = dbContext.Categories.Single(x => x.Name == "Gazli Icecekler").Id;
            var fuseTeaId = dbContext.Categories.Single(x => x.Name == "FuseTea").Id;
            var energyId = dbContext.Categories.Single(x => x.Name == "Enerji Icecekleri").Id;

            await dbContext.Products.AddRangeAsync(
            [
                new Product { CategoryId = waterAndSodaId, Name = "Damla Su (500ml)", Description = "Serinletici icme suyu.", Price = 15m },
                new Product { CategoryId = waterAndSodaId, Name = "Damla Sade Soda (200ml)", Description = "Sade maden suyu.", Price = 20m },
                new Product { CategoryId = waterAndSodaId, Name = "Damla Meyveli Soda Mango Ananas (200ml)", Description = "Mango ve ananas aromali soda.", Price = 25m },
                new Product { CategoryId = waterAndSodaId, Name = "Damla Meyveli Soda Limon (200ml)", Description = "Limon aromali soda.", Price = 25m },
                new Product { CategoryId = waterAndSodaId, Name = "Damla Meyveli Soda Karpuz (200ml)", Description = "Karpuz aromali soda.", Price = 25m },
                new Product { CategoryId = waterAndSodaId, Name = "Damla Meyveli Soda Elma (200ml)", Description = "Elma aromali soda.", Price = 25m },

                new Product { CategoryId = carbonatedId, Name = "Kutu Kola (330ml)", Description = "Klasik kutu kola.", Price = 55m },
                new Product { CategoryId = carbonatedId, Name = "Kutu Kola Sekersiz (330ml)", Description = "Sekersiz kutu kola.", Price = 55m },
                new Product { CategoryId = carbonatedId, Name = "Kutu Fanta (330ml)", Description = "Portakal aromali gazli icecek.", Price = 55m },
                new Product { CategoryId = carbonatedId, Name = "Kutu Sprite (330ml)", Description = "Limon aromali gazli icecek.", Price = 55m },
                new Product { CategoryId = carbonatedId, Name = "Sise Kola (200ml)", Description = "Cam sise kola.", Price = 35m },
                new Product { CategoryId = carbonatedId, Name = "Sise Fanta (200ml)", Description = "Cam sise fanta.", Price = 35m },
                new Product { CategoryId = carbonatedId, Name = "Sise Sprite (200ml)", Description = "Cam sise sprite.", Price = 35m },

                new Product { CategoryId = fuseTeaId, Name = "FuseTea Seftali (330ml)", Description = "Seftali aromali soguk cay.", Price = 55m },
                new Product { CategoryId = fuseTeaId, Name = "FuseTea Mango (330ml)", Description = "Mango aromali soguk cay.", Price = 55m },
                new Product { CategoryId = fuseTeaId, Name = "FuseTea Limon (330ml)", Description = "Limon aromali soguk cay.", Price = 55m },
                new Product { CategoryId = fuseTeaId, Name = "FuseTea Kavun Cilek (330ml)", Description = "Kavun ve cilek aromali soguk cay.", Price = 55m },
                new Product { CategoryId = fuseTeaId, Name = "FuseTea Karpuz (330ml)", Description = "Karpuz aromali soguk cay.", Price = 55m },

                new Product { CategoryId = energyId, Name = "BURN Enerji Icecegi (250ml)", Description = "Yuksek kafeinli enerji icecegi.", Price = 75m },
                new Product { CategoryId = energyId, Name = "Powerade (Mavi) Enerji Icecegi (500ml)", Description = "Mavi aromali performans icecegi.", Price = 75m },
                new Product { CategoryId = energyId, Name = "Powerade (Turuncu) Enerji Icecegi (500ml)", Description = "Turuncu aromali performans icecegi.", Price = 75m },
                new Product { CategoryId = energyId, Name = "Monster White Enerji Icecegi (500ml)", Description = "White serisi enerji icecegi.", Price = 75m },
                new Product { CategoryId = energyId, Name = "Monster Watermelon Enerji Icecegi (500ml)", Description = "Karpuz aromali enerji icecegi.", Price = 75m },
                new Product { CategoryId = energyId, Name = "Monster Tropical Enerji Icecegi (500ml)", Description = "Tropikal aromali enerji icecegi.", Price = 75m },
                new Product { CategoryId = energyId, Name = "Monster Pipeline Enerji Icecegi (500ml)", Description = "Pipeline Punch serisi enerji icecegi.", Price = 75m },
                new Product { CategoryId = energyId, Name = "Monster Green Enerji Icecegi (500ml)", Description = "Klasik yesil enerji icecegi.", Price = 75m },
                new Product { CategoryId = energyId, Name = "Monster Lemonade Enerji Icecegi (500ml)", Description = "Limonata aromali enerji icecegi.", Price = 75m }
            ], cancellationToken);
        }

        if (!dbContext.AppSettings.Any())
        {
            await dbContext.AppSettings.AddAsync(new AppSetting(), cancellationToken);
        }

        if (!dbContext.AdminUsers.Any())
        {
            var passwordHasher = new PasswordHasher<AdminUser>();
            var adminUser = new AdminUser
            {
                UserName = "administrator",
                DisplayName = "Administrator"
            };
            adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "Admin123!");
            await dbContext.AdminUsers.AddAsync(adminUser, cancellationToken);
        }

        if (!dbContext.InfoMessages.Any())
        {
            await dbContext.InfoMessages.AddAsync(new InfoMessage
            {
                Message = "Bu aksam 22:00 sonrasi turnuva indirimi aktif.",
                Type = InfoMessageType.Info,
                IconKey = "campaign",
                IsActive = true,
                StartDate = DateTime.UtcNow.AddMinutes(-5)
            }, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureDefaultTablesAsync(CafeOrdersDbContext dbContext, CancellationToken cancellationToken)
    {
        var tables = await dbContext.Tables
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        while (tables.Count < 90)
        {
            var nextIndex = tables.Count + 1;
            var table = new CafeTable
            {
                Name = $"Masa {nextIndex:D2}",
                Description = nextIndex <= 10 ? "VIP zone" : "Open play area",
                IsActive = true
            };
            tables.Add(table);
            await dbContext.Tables.AddAsync(table, cancellationToken);
        }

        for (var index = 0; index < Math.Min(90, tables.Count); index++)
        {
            var table = tables[index];
            table.Name = $"Masa {index + 1:D2}";
            table.Description ??= index < 10 ? "VIP zone" : "Open play area";
        }

        if (tables.Count > 90)
        {
            var canonicalTables = tables.Take(90).ToArray();
            var extraTables = tables.Skip(90).ToArray();

            foreach (var extraTable in extraTables)
            {
                var targetIndex = extraTable.Id % 90;
                var targetTable = canonicalTables[targetIndex];

                var linkedDevices = await dbContext.Devices.Where(device => device.TableId == extraTable.Id).ToListAsync(cancellationToken);
                foreach (var device in linkedDevices)
                {
                    device.TableId = targetTable.Id;
                }

                var linkedOrders = await dbContext.Orders.Where(order => order.TableId == extraTable.Id).ToListAsync(cancellationToken);
                foreach (var order in linkedOrders)
                {
                    order.TableId = targetTable.Id;
                }

                dbContext.Tables.Remove(extraTable);
            }
        }
    }
}
