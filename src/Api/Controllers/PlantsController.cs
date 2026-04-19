using Application.Common.Dtos;
using Application.Services;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlantsController : ControllerBase
{
    private readonly IPlantService _plantService;

    public PlantsController(IPlantService plantService)
    {
        _plantService = plantService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlantDto>>> GetPlants([FromQuery] SunRequirement? sunRequirement, [FromQuery] PlantingSeason? season)
    {
        return Ok(await _plantService.GetPlantsAsync(sunRequirement, season));
    }

    [HttpPost]
    public async Task<ActionResult<PlantDto>> CreatePlant(CreatePlantRequest request)
    {
        var plant = await _plantService.CreatePlantAsync(request);
        return CreatedAtAction(nameof(GetPlants), plant);
    }
}
