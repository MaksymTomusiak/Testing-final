using Application.Common.Dtos;
using Application.Services;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GardensController : ControllerBase
{
    private readonly IGardenService _gardenService;
    private readonly IPlantingService _plantingService;

    public GardensController(IGardenService gardenService, IPlantingService plantingService)
    {
        _gardenService = gardenService;
        _plantingService = plantingService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<GardenDto>>> GetGardens()
    {
        return Ok(await _gardenService.GetAllGardensAsync());
    }

    [HttpPost]
    public async Task<ActionResult<GardenDto>> CreateGarden(CreateGardenRequest request)
    {
        var garden = await _gardenService.CreateGardenAsync(request);
        return CreatedAtAction(nameof(GetGarden), new { id = garden.Id }, garden);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<GardenDto>> GetGarden(int id)
    {
        var garden = await _gardenService.GetGardenByIdAsync(id);
        if (garden == null) return NotFound();
        return Ok(garden);
    }

    [HttpPost("{id}/plantings")]
    public async Task<ActionResult<PlantingDto>> Plant(int id, CreatePlantingRequest request)
    {
        try
        {
            var planting = await _plantingService.PlantInGardenAsync(id, request);
            return Ok(planting);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id}/watering-schedule")]
    public async Task<ActionResult<IEnumerable<WateringScheduleItem>>> GetWateringSchedule(int id)
    {
        return Ok(await _plantingService.GetWateringScheduleAsync(id));
    }
}
