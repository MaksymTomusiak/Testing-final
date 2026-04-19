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

public class PlantingServiceTests
{
    private PlantingService CreateService(out AppDbContext context)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        context = new AppDbContext(options);
        return new PlantingService(context);
    }

    private async Task<(int GardenId, int PlantId)> SeedGardenAndPlant(AppDbContext context, SunExposure gardenSun, SunRequirement plantSun, PlantingSeason season, double gardenSize = 10, double plantSize = 1)
    {
        var garden = new Garden { Name = "G1", Location = "L", SizeSquareMeters = gardenSize, SoilType = SoilType.Loam, SunExposure = gardenSun };
        var plant = new Plant { Name = "P1", Species = "S", WaterFrequencyDays = 2, SunRequirement = plantSun, PlantingSeason = season, GrowthDurationDays = 10, SpaceRequiredSquareMeters = plantSize };
        
        context.Gardens.Add(garden);
        context.Plants.Add(plant);
        await context.SaveChangesAsync();
        
        return (garden.Id, plant.Id);
    }

    // --- Sun Compatibility Matrix (8 tests) ---

    [Fact]
    public async Task PlantInGarden_Throws_WhenFullSunPlantInPartialGarden()
    {
        // Arrange
        var service = CreateService(out var context);
        var (gId, pId) = await SeedGardenAndPlant(context, SunExposure.Partial, SunRequirement.Full, PlantingSeason.Spring);
        var request = new CreatePlantingRequest(pId, new DateTime(2026, 4, 1), "T");

        // Act & Assert
        await service.Invoking(s => s.PlantInGardenAsync(gId, request))
            .Should().ThrowAsync<Exception>().WithMessage("*compatible*");
    }

    [Fact]
    public async Task PlantInGarden_Throws_WhenFullSunPlantInShadeGarden()
    {
        // Arrange
        var service = CreateService(out var context);
        var (gId, pId) = await SeedGardenAndPlant(context, SunExposure.Shade, SunRequirement.Full, PlantingSeason.Spring);
        var request = new CreatePlantingRequest(pId, new DateTime(2026, 4, 1), "T");

        // Act & Assert
        await service.Invoking(s => s.PlantInGardenAsync(gId, request)).Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task PlantInGarden_Success_WhenFullSunPlantInFullGarden()
    {
        // Arrange
        var service = CreateService(out var context);
        var (gId, pId) = await SeedGardenAndPlant(context, SunExposure.Full, SunRequirement.Full, PlantingSeason.Spring);
        var request = new CreatePlantingRequest(pId, new DateTime(2026, 4, 1), "T");

        // Act
        var result = await service.PlantInGardenAsync(gId, request);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task PlantInGarden_Success_WhenPartialSunPlantInFullGarden()
    {
        // Arrange
        var service = CreateService(out var context);
        var (gId, pId) = await SeedGardenAndPlant(context, SunExposure.Full, SunRequirement.Partial, PlantingSeason.Spring);
        var request = new CreatePlantingRequest(pId, new DateTime(2026, 4, 1), "T");

        // Act
        var result = await service.PlantInGardenAsync(gId, request);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task PlantInGarden_Success_WhenPartialSunPlantInPartialGarden()
    {
        // Arrange
        var service = CreateService(out var context);
        var (gId, pId) = await SeedGardenAndPlant(context, SunExposure.Partial, SunRequirement.Partial, PlantingSeason.Spring);
        var request = new CreatePlantingRequest(pId, new DateTime(2026, 4, 1), "T");

        // Act
        var result = await service.PlantInGardenAsync(gId, request);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task PlantInGarden_Throws_WhenPartialSunPlantInShadeGarden()
    {
        // Arrange
        var service = CreateService(out var context);
        var (gId, pId) = await SeedGardenAndPlant(context, SunExposure.Shade, SunRequirement.Partial, PlantingSeason.Spring);
        var request = new CreatePlantingRequest(pId, new DateTime(2026, 4, 1), "T");

        // Act & Assert
        await service.Invoking(s => s.PlantInGardenAsync(gId, request)).Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task PlantInGarden_Success_WhenShadePlantInPartialGarden()
    {
        // Arrange
        var service = CreateService(out var context);
        var (gId, pId) = await SeedGardenAndPlant(context, SunExposure.Partial, SunRequirement.Shade, PlantingSeason.Spring);
        var request = new CreatePlantingRequest(pId, new DateTime(2026, 4, 1), "T");

        // Act
        var result = await service.PlantInGardenAsync(gId, request);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task PlantInGarden_Success_WhenShadePlantInShadeGarden()
    {
        // Arrange
        var service = CreateService(out var context);
        var (gId, pId) = await SeedGardenAndPlant(context, SunExposure.Shade, SunRequirement.Shade, PlantingSeason.Spring);
        var request = new CreatePlantingRequest(pId, new DateTime(2026, 4, 1), "T");

        // Act
        var result = await service.PlantInGardenAsync(gId, request);

        // Assert
        result.Should().NotBeNull();
    }

    // --- Season Validation Matrix (8 tests) ---

    [Fact]
    public async Task PlantInGarden_Success_SpringPlantInMarch()
    {
        // Arrange
        var service = CreateService(out var context);
        var (gId, pId) = await SeedGardenAndPlant(context, SunExposure.Full, SunRequirement.Full, PlantingSeason.Spring);
        
        // Act
        var result = await service.PlantInGardenAsync(gId, new CreatePlantingRequest(pId, new DateTime(2026, 3, 15), ""));

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task PlantInGarden_Throws_SpringPlantInJune()
    {
        // Arrange
        var service = CreateService(out var context);
        var (gId, pId) = await SeedGardenAndPlant(context, SunExposure.Full, SunRequirement.Full, PlantingSeason.Spring);
        
        // Act & Assert
        await service.Invoking(s => s.PlantInGardenAsync(gId, new CreatePlantingRequest(pId, new DateTime(2026, 6, 15), "")))
            .Should().ThrowAsync<Exception>().WithMessage("*outside its Spring season*");
    }

    [Fact]
    public async Task PlantInGarden_Success_SummerPlantInJuly()
    {
        // Arrange
        var service = CreateService(out var context);
        var (gId, pId) = await SeedGardenAndPlant(context, SunExposure.Full, SunRequirement.Full, PlantingSeason.Summer);
        
        // Act
        var result = await service.PlantInGardenAsync(gId, new CreatePlantingRequest(pId, new DateTime(2026, 7, 15), ""));

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task PlantInGarden_Throws_SummerPlantInOctober()
    {
        // Arrange
        var service = CreateService(out var context);
        var (gId, pId) = await SeedGardenAndPlant(context, SunExposure.Full, SunRequirement.Full, PlantingSeason.Summer);
        
        // Act & Assert
        await service.Invoking(s => s.PlantInGardenAsync(gId, new CreatePlantingRequest(pId, new DateTime(2026, 10, 15), ""))).Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task PlantInGarden_Success_FallPlantInOctober()
    {
        // Arrange
        var service = CreateService(out var context);
        var (gId, pId) = await SeedGardenAndPlant(context, SunExposure.Full, SunRequirement.Full, PlantingSeason.Fall);
        
        // Act
        var result = await service.PlantInGardenAsync(gId, new CreatePlantingRequest(pId, new DateTime(2026, 10, 15), ""));

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task PlantInGarden_Throws_FallPlantInDecember()
    {
        // Arrange
        var service = CreateService(out var context);
        var (gId, pId) = await SeedGardenAndPlant(context, SunExposure.Full, SunRequirement.Full, PlantingSeason.Fall);
        
        // Act & Assert
        await service.Invoking(s => s.PlantInGardenAsync(gId, new CreatePlantingRequest(pId, new DateTime(2026, 12, 15), ""))).Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task PlantInGarden_Success_WinterPlantInJanuary()
    {
        // Arrange
        var service = CreateService(out var context);
        var (gId, pId) = await SeedGardenAndPlant(context, SunExposure.Full, SunRequirement.Full, PlantingSeason.Winter);
        
        // Act
        var result = await service.PlantInGardenAsync(gId, new CreatePlantingRequest(pId, new DateTime(2026, 1, 15), ""));

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task PlantInGarden_Throws_WinterPlantInMay()
    {
        // Arrange
        var service = CreateService(out var context);
        var (gId, pId) = await SeedGardenAndPlant(context, SunExposure.Full, SunRequirement.Full, PlantingSeason.Winter);
        
        // Act & Assert
        await service.Invoking(s => s.PlantInGardenAsync(gId, new CreatePlantingRequest(pId, new DateTime(2026, 5, 15), ""))).Should().ThrowAsync<Exception>();
    }

    // --- Space Tracking (4 tests) ---

    [Fact]
    public async Task PlantInGarden_Throws_WhenPlantExceedsEmptyGardenSpace()
    {
        // Arrange
        var service = CreateService(out var context);
        var (gId, pId) = await SeedGardenAndPlant(context, SunExposure.Full, SunRequirement.Full, PlantingSeason.Spring, gardenSize: 5, plantSize: 6);
        
        // Act & Assert
        await service.Invoking(s => s.PlantInGardenAsync(gId, new CreatePlantingRequest(pId, new DateTime(2026, 4, 1), "")))
            .Should().ThrowAsync<Exception>().WithMessage("Not enough space*");
    }

    [Fact]
    public async Task PlantInGarden_Success_WhenPlantExactlyFillsGarden()
    {
        // Arrange
        var service = CreateService(out var context);
        var (gId, pId) = await SeedGardenAndPlant(context, SunExposure.Full, SunRequirement.Full, PlantingSeason.Spring, gardenSize: 5, plantSize: 5);
        
        // Act
        var result = await service.PlantInGardenAsync(gId, new CreatePlantingRequest(pId, new DateTime(2026, 4, 1), ""));

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task PlantInGarden_Throws_WhenCumulativePlantingsExceedSpace()
    {
        // Arrange
        var service = CreateService(out var context);
        var (gId, pId) = await SeedGardenAndPlant(context, SunExposure.Full, SunRequirement.Full, PlantingSeason.Spring, gardenSize: 5, plantSize: 3);
        
        await service.PlantInGardenAsync(gId, new CreatePlantingRequest(pId, new DateTime(2026, 4, 1), ""));

        // Act & Assert
        await service.Invoking(s => s.PlantInGardenAsync(gId, new CreatePlantingRequest(pId, new DateTime(2026, 4, 2), "")))
            .Should().ThrowAsync<Exception>().WithMessage("Not enough space*");
    }

    [Fact]
    public async Task PlantInGarden_Success_WhenDeadPlantsDontConsumeSpace()
    {
        // Arrange
        var service = CreateService(out var context);
        var (gId, pId) = await SeedGardenAndPlant(context, SunExposure.Full, SunRequirement.Full, PlantingSeason.Spring, gardenSize: 5, plantSize: 5);
        
        var first = await service.PlantInGardenAsync(gId, new CreatePlantingRequest(pId, new DateTime(2026, 4, 1), ""));
        await service.UpdatePlantingStatusAsync(first.Id, PlantingStatus.Dead);

        // Act
        // Should succeed because previous 5sqm is reclaimed since it's dead
        var result = await service.PlantInGardenAsync(gId, new CreatePlantingRequest(pId, new DateTime(2026, 4, 2), ""));

        // Assert
        result.Should().NotBeNull();
    }

    // --- Watering Schedule & Harvesting (5 tests) ---

    [Fact]
    public async Task GetWateringSchedule_Empty_WhenNoGrowingPlants()
    {
        // Arrange
        var service = CreateService(out var context);
        var (gId, _) = await SeedGardenAndPlant(context, SunExposure.Full, SunRequirement.Full, PlantingSeason.Spring);

        // Act
        var schedule = await service.GetWateringScheduleAsync(gId);

        // Assert
        schedule.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWateringSchedule_CalculatesIntervalsCorrectly()
    {
        // Arrange
        var service = CreateService(out var context);
        var (gId, pId) = await SeedGardenAndPlant(context, SunExposure.Full, SunRequirement.Full, PlantingSeason.Spring);
        
        // Planted 5 days ago, WaterFrequency is 2 days. Next dates: Today+1, Today+3... up to 30 days.
        var plantedDate = DateTime.Today.AddDays(-5);
        await service.PlantInGardenAsync(gId, new CreatePlantingRequest(pId, plantedDate, ""));

        // Act
        var schedule = await service.GetWateringScheduleAsync(gId);
        
        // Assert
        schedule.Should().NotBeEmpty();
        schedule.Should().OnlyContain(s => s.Date >= DateTime.Today && s.Date <= DateTime.Today.AddDays(30));
    }

    [Fact]
    public async Task UpdatePlantingStatusAsync_Throws_WhenNotFound()
    {
        // Arrange
        var service = CreateService(out _);

        // Act & Assert
        await service.Invoking(s => s.UpdatePlantingStatusAsync(99, PlantingStatus.Harvested))
            .Should().ThrowAsync<Exception>().WithMessage("*not found*");
    }

    [Fact]
    public async Task GetReadyToHarvestAsync_ReturnsOnlyGrowingPlantsPastHarvestDate()
    {
        // Arrange
        var service = CreateService(out var context);
        var (gId, pId) = await SeedGardenAndPlant(context, SunExposure.Full, SunRequirement.Full, PlantingSeason.Spring);
        
        // Growth duration is 10 days. Planted 15 days ago -> Past harvest
        var plantedDate = DateTime.Today.AddDays(-15);
        await service.PlantInGardenAsync(gId, new CreatePlantingRequest(pId, plantedDate, ""));

        // Act
        var result = await service.GetReadyToHarvestAsync(1, 10);

        // Assert
        result.Items.Should().ContainSingle();
        result.Items.First().ExpectedHarvestDate.Should().BeBefore(DateTime.Today.AddDays(1));
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetReadyToHarvestAsync_IgnoresDeadOrHarvestedPlants()
    {
        // Arrange
        var service = CreateService(out var context);
        var (gId, pId) = await SeedGardenAndPlant(context, SunExposure.Full, SunRequirement.Full, PlantingSeason.Spring);
        
        var p = await service.PlantInGardenAsync(gId, new CreatePlantingRequest(pId, DateTime.Today.AddDays(-15), ""));
        await service.UpdatePlantingStatusAsync(p.Id, PlantingStatus.Harvested);

        // Act
        var result = await service.GetReadyToHarvestAsync(1, 10);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetReadyToHarvestAsync_HandlesPaginationCorrectly()
    {
        // Arrange
        var service = CreateService(out var context);
        var (gId, pId) = await SeedGardenAndPlant(context, SunExposure.Full, SunRequirement.Full, PlantingSeason.Spring);
        
        // Seed 5 ready-to-harvest plants
        var plantedDate = DateTime.Today.AddDays(-15);
        for (int i = 0; i < 5; i++)
        {
            await service.PlantInGardenAsync(gId, new CreatePlantingRequest(pId, plantedDate, $"Plant {i}"));
        }

        // Act - Page 1, Size 2
        var page1 = await service.GetReadyToHarvestAsync(1, 2);
        // Act - Page 2, Size 2
        var page2 = await service.GetReadyToHarvestAsync(2, 2);
        // Act - Page 3, Size 2
        var page3 = await service.GetReadyToHarvestAsync(3, 2);

        // Assert
        page1.Items.Should().HaveCount(2);
        page1.TotalCount.Should().Be(5);
        page1.TotalPages.Should().Be(3);
        page1.PageNumber.Should().Be(1);

        page2.Items.Should().HaveCount(2);
        page2.PageNumber.Should().Be(2);

        page3.Items.Should().HaveCount(1);
        page3.PageNumber.Should().Be(3);
    }
}
