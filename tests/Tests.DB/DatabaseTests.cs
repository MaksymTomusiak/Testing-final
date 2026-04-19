using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Tests.DB;

public class DatabaseTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithDatabase("garden_db")
        .WithUsername("garden_user")
        .WithPassword("garden_pass")
        .Build();

    public async Task InitializeAsync() => await _container.StartAsync();
    public async Task DisposeAsync() => await _container.DisposeAsync().AsTask();

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task Test_Database_Relationships_and_Cleanup()
    {
        // Arrange
        using var context = CreateContext();

        // 1. Add Garden
        var garden = new Garden { Name = "Main Garden", SizeSquareMeters = 100, SunExposure = SunExposure.Full };
        context.Gardens.Add(garden);
        await context.SaveChangesAsync();

        // 2. Add Plant
        var plant = new Plant { Name = "Tomato", Species = "Solanum", GrowthDurationDays = 30, SunRequirement = SunRequirement.Full };
        context.Plants.Add(plant);
        await context.SaveChangesAsync();

        // 3. Add Planting
        var planting = new Planting 
        { 
            GardenId = garden.Id, 
            PlantId = plant.Id, 
            PlantedDate = DateTime.UtcNow.Date, 
            Status = PlantingStatus.Growing,
            ExpectedHarvestDate = DateTime.UtcNow.Date.AddDays(30)
        };
        context.Plantings.Add(planting);
        await context.SaveChangesAsync();

        // 4. Verify Relationships
        // Act
        var result = await context.Gardens
            .Include(g => g.Plantings)
            .ThenInclude(p => p.Plant)
            .FirstOrDefaultAsync(g => g.Id == garden.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Plantings);
        Assert.Equal("Tomato", result.Plantings.First().Plant.Name);
    }

    [Fact]
    public async Task Db_CreateGarden_CorrectlySavesEnums()
    {
        // Arrange
        using var context = CreateContext();
        var garden = new Garden { Name = "Enum Test", SoilType = SoilType.Sandy, SunExposure = SunExposure.Shade };
        context.Gardens.Add(garden);
        await context.SaveChangesAsync();

        // Act
        var inDb = await context.Gardens.FindAsync(garden.Id);

        // Assert
        Assert.Equal(SoilType.Sandy, inDb!.SoilType);
        Assert.Equal(SunExposure.Shade, inDb.SunExposure);
    }

    [Fact]
    public async Task Db_DeleteGarden_ShouldCascadeDeletePlantings()
    {
        // Arrange
        using var context = CreateContext();
        var garden = new Garden { Name = "Cascade Test", SizeSquareMeters = 10, SunExposure = SunExposure.Full };
        var plant = new Plant { Name = "P", Species = "S", SunRequirement = SunRequirement.Full };
        context.Gardens.Add(garden);
        context.Plants.Add(plant);
        await context.SaveChangesAsync();

        var planting = new Planting { GardenId = garden.Id, PlantId = plant.Id, PlantedDate = DateTime.UtcNow.Date, ExpectedHarvestDate = DateTime.UtcNow.Date.AddDays(1) };
        context.Plantings.Add(planting);
        await context.SaveChangesAsync();

        // Act
        // Delete Garden
        context.Gardens.Remove(garden);
        await context.SaveChangesAsync();

        // Assert
        var plantingsCount = await context.Plantings.CountAsync(p => p.GardenId == garden.Id);
        Assert.Equal(0, plantingsCount);
    }

    [Fact]
    public async Task Db_AddPlant_WithRequiredSpace_ShouldPersistCorrectly()
    {
        // Arrange
        using var context = CreateContext();
        var plant = new Plant { Name = "Space Plant", SpaceRequiredSquareMeters = 0.75, SunRequirement = SunRequirement.Full };
        context.Plants.Add(plant);
        await context.SaveChangesAsync();

        // Act
        var inDb = await context.Plants.FindAsync(plant.Id);

        // Assert
        Assert.Equal(0.75, inDb!.SpaceRequiredSquareMeters);
    }

    [Fact]
    public async Task Db_UpdatePlanting_ShouldReflectChangesInDb()
    {
        // Arrange
        using var context = CreateContext();
        var garden = new Garden { Name = "G", SunExposure = SunExposure.Full };
        var plant = new Plant { Name = "P", SunRequirement = SunRequirement.Full };
        context.Gardens.Add(garden);
        context.Plants.Add(plant);
        await context.SaveChangesAsync();

        var planting = new Planting { GardenId = garden.Id, PlantId = plant.Id, PlantedDate = DateTime.UtcNow.Date, Status = PlantingStatus.Growing };
        context.Plantings.Add(planting);
        await context.SaveChangesAsync();

        // Act
        // Update
        planting.Status = PlantingStatus.Harvested;
        await context.SaveChangesAsync();

        // Assert
        context.Entry(planting).State = EntityState.Detached;
        var inDb = await context.Plantings.FindAsync(planting.Id);
        Assert.Equal(PlantingStatus.Harvested, inDb!.Status);
    }

    [Fact]
    public async Task Db_GetReadyToHarvest_QueryMatchesCorrectLogic()
    {
        // Arrange
        using var context = CreateContext();
        var garden = new Garden { Name = "G", SunExposure = SunExposure.Full };
        var plant = new Plant { Name = "P", SunRequirement = SunRequirement.Full };
        context.Gardens.Add(garden);
        context.Plants.Add(plant);
        await context.SaveChangesAsync();

        // 1. One past harvest
        context.Plantings.Add(new Planting { GardenId = garden.Id, PlantId = plant.Id, Status = PlantingStatus.Growing, ExpectedHarvestDate = DateTime.UtcNow.Date.AddDays(-1) });
        // 2. One in future
        context.Plantings.Add(new Planting { GardenId = garden.Id, PlantId = plant.Id, Status = PlantingStatus.Growing, ExpectedHarvestDate = DateTime.UtcNow.Date.AddDays(1) });
        // 3. One already harvested
        context.Plantings.Add(new Planting { GardenId = garden.Id, PlantId = plant.Id, Status = PlantingStatus.Harvested, ExpectedHarvestDate = DateTime.UtcNow.Date.AddDays(-1) });
        
        await context.SaveChangesAsync();

        // Act
        var readyList = await context.Plantings
            .Where(p => p.Status == PlantingStatus.Growing && p.ExpectedHarvestDate <= DateTime.UtcNow.Date)
            .ToListAsync();

        // Assert
        Assert.Single(readyList);
    }
}
