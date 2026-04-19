using Application.Common.Dtos;
using Domain.Entities;
using Domain.Enums;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public interface IPlantingService
{
    Task<PlantingDto> PlantInGardenAsync(int gardenId, CreatePlantingRequest request);
    Task UpdatePlantingStatusAsync(int plantingId, PlantingStatus status);
    Task<IEnumerable<WateringScheduleItem>> GetWateringScheduleAsync(int gardenId);
    Task<PaginatedList<PlantingDto>> GetReadyToHarvestAsync(int pageNumber, int pageSize);
}

public class PlantingService : IPlantingService
{
    private readonly IAppDbContext _context;

    public PlantingService(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<PlantingDto> PlantInGardenAsync(int gardenId, CreatePlantingRequest request)
    {
        var garden = await _context.Gardens
                         .Include(g => g.Plantings)
                         .ThenInclude(p => p.Plant)
                         .FirstOrDefaultAsync(g => g.Id == gardenId)
                     ?? throw new Exception("Garden not found");

        var plant = await _context.Plants.FindAsync(request.PlantId)
                    ?? throw new Exception("Plant not found");

        if (!IsSunCompatible(plant.SunRequirement, garden.SunExposure))
            throw new Exception("Plant sun requirements are not compatible with garden sun exposure");

        if (!IsCorrectSeason(request.PlantedDate, plant.PlantingSeason))
            throw new Exception(
                $"Cannot plant {plant.Name} in {request.PlantedDate:MMMM} as it is outside its {plant.PlantingSeason} season");

        var usedSpace = garden.Plantings.Where(p => p.Status == PlantingStatus.Growing)
            .Sum(p => p.Plant.SpaceRequiredSquareMeters);
        if (usedSpace + plant.SpaceRequiredSquareMeters > garden.SizeSquareMeters)
            throw new Exception("Not enough space in the garden");

        var planting = new Planting
        {
            GardenId = gardenId,
            PlantId = request.PlantId,
            PlantedDate = request.PlantedDate,
            ExpectedHarvestDate = request.PlantedDate.AddDays(plant.GrowthDurationDays),
            Status = PlantingStatus.Growing,
            Notes = request.Notes
        };

        _context.Plantings.Add(planting);
        await _context.SaveChangesAsync();

        return new PlantingDto(planting.Id, planting.GardenId, planting.PlantId, planting.PlantedDate,
            planting.ExpectedHarvestDate, planting.Status, planting.Notes,
            new PlantDto(plant.Id, plant.Name, plant.Species, plant.WaterFrequencyDays, plant.SunRequirement,
                plant.GrowthDurationDays, plant.PlantingSeason, plant.SpaceRequiredSquareMeters));
    }

    public async Task UpdatePlantingStatusAsync(int plantingId, PlantingStatus status)
    {
        var planting = await _context.Plantings.FindAsync(plantingId)
                       ?? throw new Exception("Planting not found");

        planting.Status = status;
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<WateringScheduleItem>> GetWateringScheduleAsync(int gardenId)
    {
        var startDate = DateTime.UtcNow.Date;
        var endDate = startDate.AddDays(30);

        // 1. PROJECTION: We only ask SQL for the exact 4 columns we need.
        // This entirely bypasses EF's heavy tracking mechanics and drastically reduces RAM usage.
        var activePlantings = await _context.Plantings
            .Where(p => p.GardenId == gardenId && p.Status == PlantingStatus.Growing)
            .Select(p => new
            {
                p.Id,
                p.PlantedDate,
                PlantName = p.Plant.Name,
                p.Plant.WaterFrequencyDays
            })
            .ToListAsync();

        var schedule = new List<WateringScheduleItem>();

        foreach (var planting in activePlantings)
        {
            var nextWatering = planting.PlantedDate;
            while (nextWatering <= endDate)
            {
                if (nextWatering >= startDate)
                {
                    schedule.Add(new WateringScheduleItem(nextWatering, planting.PlantName, planting.Id));
                }

                nextWatering = nextWatering.AddDays(planting.WaterFrequencyDays);
            }
        }

        return schedule.OrderBy(s => s.Date);
    }

    public async Task<PaginatedList<PlantingDto>> GetReadyToHarvestAsync(int pageNumber, int pageSize)
    {
        var query = _context.Plantings
            .AsNoTracking()
            .Where(p => p.Status == PlantingStatus.Growing && p.ExpectedHarvestDate <= DateTime.UtcNow.Date);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PlantingDto(p.Id, p.GardenId, p.PlantId, p.PlantedDate, p.ExpectedHarvestDate, p.Status,
                p.Notes,
                new PlantDto(p.Plant.Id, p.Plant.Name, p.Plant.Species, p.Plant.WaterFrequencyDays,
                    p.Plant.SunRequirement, p.Plant.GrowthDurationDays, p.Plant.PlantingSeason,
                    p.Plant.SpaceRequiredSquareMeters)))
            .ToListAsync();

        return new PaginatedList<PlantingDto>(items, totalCount, pageNumber, pageSize, (int)Math.Ceiling(totalCount / (double)pageSize));
    }

    private bool IsSunCompatible(SunRequirement plantReq, SunExposure gardenExp)
    {
        return plantReq switch
        {
            SunRequirement.Full => gardenExp == SunExposure.Full,
            SunRequirement.Partial => gardenExp == SunExposure.Full || gardenExp == SunExposure.Partial,
            SunRequirement.Shade => gardenExp == SunExposure.Partial || gardenExp == SunExposure.Shade,
            _ => false
        };
    }

    private bool IsCorrectSeason(DateTime date, PlantingSeason season)
    {
        var month = date.Month;
        return season switch
        {
            PlantingSeason.Spring => month is >= 3 and <= 5,
            PlantingSeason.Summer => month is >= 6 and <= 8,
            PlantingSeason.Fall => month is >= 9 and <= 11,
            PlantingSeason.Winter => month is 12 or 1 or 2,
            _ => false
        };
    }
}