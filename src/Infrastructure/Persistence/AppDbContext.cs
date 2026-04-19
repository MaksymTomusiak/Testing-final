using Domain.Entities;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Garden> Gardens { get; set; } = null!;
    public DbSet<Plant> Plants { get; set; } = null!;
    public DbSet<Planting> Plantings { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Planting>()
            .HasOne(p => p.Garden)
            .WithMany(g => g.Plantings)
            .HasForeignKey(p => p.GardenId);

        modelBuilder.Entity<Planting>()
            .HasOne(p => p.Plant)
            .WithMany()
            .HasForeignKey(p => p.PlantId);
    }
}
