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

public class GardenServiceTests
{
    private GardenService CreateService(out AppDbContext context)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        context = new AppDbContext(options);
        return new GardenService(context);
    }

    [Fact]
    public async Task GetAllGardensAsync_ShouldReturnEmpty_WhenNoGardens()
    {
        // Arrange
        var service = CreateService(out _);

        // Act
        var result = await service.GetAllGardensAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllGardensAsync_ShouldReturnAllGardens()
    {
        // Arrange
        var service = CreateService(out var context);
        context.Gardens.Add(new Garden { Name = "G1", Location = "L1", SizeSquareMeters = 10, SoilType = SoilType.Loam, SunExposure = SunExposure.Full });
        context.Gardens.Add(new Garden { Name = "G2", Location = "L2", SizeSquareMeters = 20, SoilType = SoilType.Clay, SunExposure = SunExposure.Partial });
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAllGardensAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Select(x => x.Name).Should().Contain(new[] { "G1", "G2" });
    }

    [Fact]
    public async Task GetGardenByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var service = CreateService(out _);

        // Act
        var result = await service.GetGardenByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetGardenByIdAsync_ShouldReturnGarden_WhenExists()
    {
        // Arrange
        var service = CreateService(out var context);
        var garden = new Garden { Name = "G1", Location = "L1", SizeSquareMeters = 10, SoilType = SoilType.Loam, SunExposure = SunExposure.Full };
        context.Gardens.Add(garden);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetGardenByIdAsync(garden.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("G1");
    }

    [Fact]
    public async Task CreateGardenAsync_ShouldPersistSuccessfully()
    {
        // Arrange
        var service = CreateService(out var context);
        var request = new CreateGardenRequest("New Garden", "Backyard", 50, SoilType.Sandy, SunExposure.Full);
        
        // Act
        var result = await service.CreateGardenAsync(request);
        
        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("New Garden");

        var inDb = await context.Gardens.FindAsync(result.Id);
        inDb.Should().NotBeNull();
    }
}
