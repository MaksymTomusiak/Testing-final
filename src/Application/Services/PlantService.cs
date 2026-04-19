using Application.Common.Dtos;
using Domain.Entities;
using Domain.Enums;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public interface IPlantService
{
    Task<IEnumerable<PlantDto>> GetPlantsAsync(SunRequirement? sunRequirement, PlantingSeason? season);
    Task<PlantDto> CreatePlantAsync(CreatePlantRequest request);
}

public class PlantService : IPlantService
{
    private readonly IAppDbContext _context;

    public PlantService(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PlantDto>> GetPlantsAsync(SunRequirement? sunRequirement, PlantingSeason? season)
    {
        var query = _context.Plants.AsQueryable();

        if (sunRequirement.HasValue)
            query = query.Where(p => p.SunRequirement == sunRequirement.Value);

        if (season.HasValue)
            query = query.Where(p => p.PlantingSeason == season.Value);

        return await query
            .Select(p => new PlantDto(p.Id, p.Name, p.Species, p.WaterFrequencyDays, p.SunRequirement, p.GrowthDurationDays, p.PlantingSeason, p.SpaceRequiredSquareMeters))
            .ToListAsync();
    }

    public async Task<PlantDto> CreatePlantAsync(CreatePlantRequest request)
    {
        var plant = new Plant
        {
            Name = request.Name,
            Species = request.Species,
            WaterFrequencyDays = request.WaterFrequencyDays,
            SunRequirement = request.SunRequirement,
            GrowthDurationDays = request.GrowthDurationDays,
            PlantingSeason = request.PlantingSeason,
            SpaceRequiredSquareMeters = request.SpaceRequiredSquareMeters
        };

        _context.Plants.Add(plant);
        await _context.SaveChangesAsync();

        return new PlantDto(plant.Id, plant.Name, plant.Species, plant.WaterFrequencyDays, plant.SunRequirement, plant.GrowthDurationDays, plant.PlantingSeason, plant.SpaceRequiredSquareMeters);
    }
}
