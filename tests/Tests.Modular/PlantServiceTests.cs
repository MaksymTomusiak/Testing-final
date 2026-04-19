using Application.Common.Dtos;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Application.Interfaces;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests.Modular;

public class PlantServiceTests
{
    private PlantService CreateService(out AppDbContext context)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        context = new AppDbContext(options);
        return new PlantService(context);
    }

    private async Task SeedPlants(AppDbContext context)
    {
        context.Plants.AddRange(
            new Plant { Name = "Tomato", Species = "Solanum lycopersicum", WaterFrequencyDays = 2, SunRequirement = SunRequirement.Full, PlantingSeason = PlantingSeason.Spring, GrowthDurationDays = 80, SpaceRequiredSquareMeters = 0.5 },
            new Plant { Name = "Lettuce", Species = "Lactuca sativa", WaterFrequencyDays = 1, SunRequirement = SunRequirement.Partial, PlantingSeason = PlantingSeason.Spring, GrowthDurationDays = 45, SpaceRequiredSquareMeters = 0.1 },
            new Plant { Name = "Kale", Species = "Brassica oleracea", WaterFrequencyDays = 3, SunRequirement = SunRequirement.Partial, PlantingSeason = PlantingSeason.Fall, GrowthDurationDays = 60, SpaceRequiredSquareMeters = 0.3 },
            new Plant { Name = "Mint", Species = "Mentha", WaterFrequencyDays = 4, SunRequirement = SunRequirement.Shade, PlantingSeason = PlantingSeason.Summer, GrowthDurationDays = 30, SpaceRequiredSquareMeters = 0.2 }
        );
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetPlantsAsync_WithNoFilters_ShouldReturnAll()
    {
        // Arrange
        var service = CreateService(out var context);
        await SeedPlants(context);

        // Act
        var result = await service.GetPlantsAsync(null, null);

        // Assert
        result.Should().HaveCount(4);
    }

    [Fact]
    public async Task GetPlantsAsync_WithSunFilter_ShouldReturnCorrect()
    {
        // Arrange
        var service = CreateService(out var context);
        await SeedPlants(context);

        // Act
        var result = await service.GetPlantsAsync(SunRequirement.Partial, null);
    
        // Assert
        // This asserts the count is exactly 2 AND it contains only these exact items
        result.Select(x => x.Name).Should().BeEquivalentTo("Lettuce", "Kale");
    
        result.Should().NotContain(x => x.Name == "Tomato");
    }
    
    [Fact]
    public async Task GetPlantsAsync_WithSeasonFilter_ShouldReturnCorrect()
    {
        // Arrange
        var service = CreateService(out var context);
        await SeedPlants(context);

        // Act
        var result = await service.GetPlantsAsync(null, PlantingSeason.Fall);

        // Assert
        result.Should().ContainSingle();
        result.First().Name.Should().Be("Kale");
    }

    [Fact]
    public async Task GetPlantsAsync_WithBothFilters_ShouldReturnIntersection()
    {
        // Arrange
        var service = CreateService(out var context);
        await SeedPlants(context);

        // Act
        var result = await service.GetPlantsAsync(SunRequirement.Partial, PlantingSeason.Spring);

        // Assert
        result.Should().ContainSingle();
        result.First().Name.Should().Be("Lettuce");
    }

    [Fact]
    public async Task CreatePlantAsync_ShouldPersistSuccessfully()
    {
        // Arrange
        var service = CreateService(out var context);
        var request = new CreatePlantRequest("Carrot", "Daucus carota", 3, SunRequirement.Full, 75, PlantingSeason.Spring, 0.05);

        // Act
        var result = await service.CreatePlantAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("Carrot");

        var inDb = await context.Plants.FindAsync(result.Id);
        inDb.Should().NotBeNull();
    }
}
