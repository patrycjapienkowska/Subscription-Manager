using Microsoft.EntityFrameworkCore;
using LifeAdmin.Api.Models;
namespace LifeAdmin.Api.Data;


public class  AppDbContext : DbContext //DbContext to główna klasa Entity Framework, która odpowiada za komunikację aplikacji z bazą danych.
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        
    }
    public DbSet<Subscription> Subscriptions { get; set; } //DbSet reprezentuje kolekcję wszystkich encji w kontekście lub jednostce pracy, które mogą być zapytane z bazy danych. W tym przypadku, DbSet<Subscription> reprezentuje tabelę Subscriptions w bazie danych.

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.Property(x => x.Username).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.PasswordHash).IsRequired();
        });

        modelBuilder.Entity<Subscription>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            e.Property(x => x.Price).HasColumnType("decimal(18,2)");
            e.Property(x => x.BillingCycle).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.UserId);
        });
    }
}