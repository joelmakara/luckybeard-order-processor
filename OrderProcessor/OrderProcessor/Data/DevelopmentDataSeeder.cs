using Microsoft.EntityFrameworkCore;
using RefactoringExercise.Models;

namespace RefactoringExercise.Data;

public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(OrdersDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        if (await db.Customers.AnyAsync())
        {
            return;
        }

        db.Customers.Add(new Customer { Name = "Ada Lovelace", Email = "ada@example.com" });
        db.Products.AddRange(
            new Product { Name = "Keyboard", Price = 49.99m },
            new Product { Name = "Mouse", Price = 24.50m },
            new Product { Name = "Monitor", Price = 189.00m });

        await db.SaveChangesAsync();
    }
}
