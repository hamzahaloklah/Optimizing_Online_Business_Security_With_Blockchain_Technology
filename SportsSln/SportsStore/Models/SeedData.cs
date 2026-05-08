using Microsoft.EntityFrameworkCore;
namespace SportsStore.Models
{
    public static class SeedData
    {
        public static void EnsurePopulated (IApplicationBuilder app)
        {
            StoreDbContext context = app.ApplicationServices.CreateScope().
                ServiceProvider.GetRequiredService<StoreDbContext>();
            if (context.Database.GetPendingMigrations().Any())
            {
                context.Database.Migrate();
            }

            if (!context.Products.Any())
            {
                context.Products.AddRange(
                    new Product {Name="Product01", Description="Desc01", Category="Cat01", Price=59.9m},
                    new Product { Name = "Product02", Description = "Desc02", Category = "Cat01", Price = 20.6m },
                    new Product { Name = "Product03", Description = "Desc03", Category = "Cat01", Price = 380m },
                    new Product { Name = "Product04", Description = "Desc04", Category = "Cat02", Price = 36.2m },
                    new Product { Name = "Product05", Description = "Desc05", Category = "Cat02", Price = 9800m },
                    new Product { Name = "Product06", Description = "Desc06", Category = "Cat02", Price = 27m },
                    new Product { Name = "Product07", Description = "Desc07", Category = "Cat03", Price = 30.85m },
                    new Product { Name = "Product08", Description = "Desc08", Category = "Cat03", Price = 86m },
                    new Product { Name = "Product09", Description = "Desc09", Category = "Cat03", Price = 2311m }
                    );
                context.SaveChanges();
            }
        }
    }
}
