using Application.Common.Dtos;
using Application.Services;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlantingsController : ControllerBase
{
    private readonly IPlantingService _plantingService;

    public PlantingsController(IPlantingService plantingService)
    {
        _plantingService = plantingService;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateStatus(int id, UpdatePlantingStatusRequest request)
    {
        try
        {
            await _plantingService.UpdatePlantingStatusAsync(id, request.Status);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("ready-to-harvest")]
    public async Task<ActionResult<PaginatedList<PlantingDto>>> GetReadyToHarvest([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        return Ok(await _plantingService.GetReadyToHarvestAsync(pageNumber, pageSize));
    }
}
