using Microsoft.EntityFrameworkCore;
using RefactoringExercise.Models;

namespace RefactoringExercise.Data;

public class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(customer =>
        {
            customer.Property(c => c.Name).HasMaxLength(200);
            customer.Property(c => c.Email).HasMaxLength(320);
        });

        modelBuilder.Entity<Order>(order =>
        {
            order.Property(o => o.Total).HasPrecision(18, 2);
            order.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
            order.Property(o => o.PaymentMethod).HasConversion<string>().HasMaxLength(20);
            order.HasOne<Customer>().WithMany().HasForeignKey(o => o.CustomerId);
        });

        modelBuilder.Entity<Product>(product =>
        {
            product.Property(p => p.Price).HasPrecision(18, 2);
            product.Property(p => p.Name).HasMaxLength(200);
            product.HasIndex(p => p.Name).IsUnique();
        });
    }
}
