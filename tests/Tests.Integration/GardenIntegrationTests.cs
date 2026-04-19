using System.Net;
using System.Net.Http.Json;
using Application.Common.Dtos;
using Domain.Enums;
using FluentAssertions;

namespace Tests.Integration;

public class GardenIntegrationTests : IClassFixture<DockerApiFixture>, IAsyncLifetime
{
    private readonly DockerApiFixture _fixture;
    private HttpClient _client = null!;

    public GardenIntegrationTests(DockerApiFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _client = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    // --- Garden API Tests ---

    [Fact]
    public async Task GetGardens_ShouldReturnList()
    {
        // Act
        var res = await _client.GetAsync("/api/gardens");

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var gardens = await res.Content.ReadFromJsonAsync<IEnumerable<GardenDto>>();
        gardens.Should().NotBeNull();
    }

    [Fact]
    public async Task GetGardenById_ShouldReturnGarden_WhenExists()
    {
        // Arrange
        var request = new CreateGardenRequest("Integration Garden 2", "Kiev", 50, SoilType.Loam, SunExposure.Full);
        var createRes = await _client.PostAsJsonAsync("/api/gardens", request);
        var created = await createRes.Content.ReadFromJsonAsync<GardenDto>();

        // Act
        var res = await _client.GetAsync($"/api/gardens/{created!.Id}");

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var garden = await res.Content.ReadFromJsonAsync<GardenDto>();
        garden!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetGardenById_ShouldReturnNotFound_WhenMissing()
    {
        // Act
        var res = await _client.GetAsync("/api/gardens/9999");

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateGarden_ShouldBeSuccessful()
    {
        // Arrange
        var request = new CreateGardenRequest("Integration Garden", "Kiev", 50, SoilType.Loam, SunExposure.Full);

        // Act
        var res = await _client.PostAsJsonAsync("/api/gardens", request);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var garden = await res.Content.ReadFromJsonAsync<GardenDto>();
        garden.Should().NotBeNull();
        garden!.Name.Should().Be("Integration Garden");
    }

    // --- Plant API Tests ---

    [Fact]
    public async Task GetPlants_ShouldReturnList()
    {
        // Act
        var res = await _client.GetAsync("/api/plants");

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreatePlant_ShouldBeSuccessful()
    {
        // Arrange
        var request = new CreatePlantRequest("Integration Tomato", "Solanum", 2, SunRequirement.Full, 60, PlantingSeason.Spring, 0.5);

        // Act
        var res = await _client.PostAsJsonAsync("/api/plants", request);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // --- Planting & Business Rules API Tests ---

    [Fact]
    public async Task PlantInGarden_ShouldReturnOk_WhenAllRulesPass()
    {
        // Arrange
        var gRes = await _client.PostAsJsonAsync("/api/gardens", new CreateGardenRequest("G", "L", 100, SoilType.Loam, SunExposure.Full));
        var garden = await gRes.Content.ReadFromJsonAsync<GardenDto>();

        var pRes = await _client.PostAsJsonAsync("/api/plants", new CreatePlantRequest("P", "S", 1, SunRequirement.Full, 10, PlantingSeason.Spring, 1));
        var plant = await pRes.Content.ReadFromJsonAsync<PlantDto>();

        // Act
        var res = await _client.PostAsJsonAsync($"/api/gardens/{garden!.Id}/plantings", new CreatePlantingRequest(plant!.Id, new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc), "Successfully planted"));
        
        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var planting = await res.Content.ReadFromJsonAsync<PlantingDto>();
        planting.Should().NotBeNull();
    }

    [Fact]
    public async Task PlantInGarden_ShouldReturnBadRequest_WhenSunIncompatible()
    {
        // Arrange
        // 1. Create Shade Garden
        var gRes = await _client.PostAsJsonAsync("/api/gardens", new CreateGardenRequest("Shade G", "L", 10, SoilType.Loam, SunExposure.Shade));
        var garden = await gRes.Content.ReadFromJsonAsync<GardenDto>();

        // 2. Create Full Sun Plant
        var pRes = await _client.PostAsJsonAsync("/api/plants", new CreatePlantRequest("Sun P", "S", 1, SunRequirement.Full, 10, PlantingSeason.Spring, 1));
        var plant = await pRes.Content.ReadFromJsonAsync<PlantDto>();

        // Act
        // 3. Try to plant
        var res = await _client.PostAsJsonAsync($"/api/gardens/{garden!.Id}/plantings", new CreatePlantingRequest(plant!.Id, new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc), ""));
        
        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await res.Content.ReadAsStringAsync();
        error.Should().Contain("compatible");
    }

    [Fact]
    public async Task PlantInGarden_ShouldReturnBadRequest_WhenSeasonIncorrect()
    {
        // Arrange
        var gRes = await _client.PostAsJsonAsync("/api/gardens", new CreateGardenRequest("G", "L", 10, SoilType.Loam, SunExposure.Full));
        var garden = await gRes.Content.ReadFromJsonAsync<GardenDto>();

        var pRes = await _client.PostAsJsonAsync("/api/plants", new CreatePlantRequest("Winter P", "S", 1, SunRequirement.Full, 10, PlantingSeason.Winter, 1));
        var plant = await pRes.Content.ReadFromJsonAsync<PlantDto>();

        // Act
        // Plant in April (Spring) -> Should fail
        var res = await _client.PostAsJsonAsync($"/api/gardens/{garden!.Id}/plantings", new CreatePlantingRequest(plant!.Id, new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc), ""));
        
        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await res.Content.ReadAsStringAsync();
        error.Should().Contain("season");
    }

    [Fact]
    public async Task PlantInGarden_ShouldReturnBadRequest_WhenNoSpace()
    {
        // Arrange
        var gRes = await _client.PostAsJsonAsync("/api/gardens", new CreateGardenRequest("Small G", "L", 1, SoilType.Loam, SunExposure.Full));
        var garden = await gRes.Content.ReadFromJsonAsync<GardenDto>();

        var pRes = await _client.PostAsJsonAsync("/api/plants", new CreatePlantRequest("Big P", "S", 1, SunRequirement.Full, 10, PlantingSeason.Spring, 2));
        var plant = await pRes.Content.ReadFromJsonAsync<PlantDto>();

        // Act
        var res = await _client.PostAsJsonAsync($"/api/gardens/{garden!.Id}/plantings", new CreatePlantingRequest(plant!.Id, new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc), ""));
        
        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await res.Content.ReadAsStringAsync();
        error.Should().Contain("space");
    }

    [Fact]
    public async Task UpdatePlantingStatus_ShouldReturnNoContent()
    {
        // Arrange
        // 1. Setup
        var gRes = await _client.PostAsJsonAsync("/api/gardens", new CreateGardenRequest("G", "L", 10, SoilType.Loam, SunExposure.Full));
        var garden = await gRes.Content.ReadFromJsonAsync<GardenDto>();
        var pRes = await _client.PostAsJsonAsync("/api/plants", new CreatePlantRequest("P", "S", 1, SunRequirement.Full, 10, PlantingSeason.Spring, 1));
        var plant = await pRes.Content.ReadFromJsonAsync<PlantDto>();
        var plRes = await _client.PostAsJsonAsync($"/api/gardens/{garden!.Id}/plantings", new CreatePlantingRequest(plant!.Id, new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc), ""));
        var planting = await plRes.Content.ReadFromJsonAsync<PlantingDto>();

        // Act
        // 2. Update
        var res = await _client.PutAsJsonAsync($"/api/plantings/{planting!.Id}", new UpdatePlantingStatusRequest(PlantingStatus.Harvested));

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetWateringSchedule_ShouldReturnOk()
    {
        // Arrange
        var gRes = await _client.PostAsJsonAsync("/api/gardens", new CreateGardenRequest("G", "L", 10, SoilType.Loam, SunExposure.Full));
        var garden = await gRes.Content.ReadFromJsonAsync<GardenDto>();
        
        // Act
        var res = await _client.GetAsync($"/api/gardens/{garden!.Id}/watering-schedule");

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetReadyToHarvest_ShouldReturnOk()
    {
        // Act
        var res = await _client.GetAsync("/api/plantings/ready-to-harvest?pageNumber=1&pageSize=10");

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await res.Content.ReadFromJsonAsync<PaginatedList<PlantingDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
    }
}