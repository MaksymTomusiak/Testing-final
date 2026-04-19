using Application.Common.Dtos;
using Domain.Entities;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public interface IGardenService
{
    Task<IEnumerable<GardenDto>> GetAllGardensAsync();
    Task<GardenDto> CreateGardenAsync(CreateGardenRequest request);
    Task<GardenDto?> GetGardenByIdAsync(int id);
}

public class GardenService : IGardenService
{
    private readonly IAppDbContext _context;

    public GardenService(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<GardenDto>> GetAllGardensAsync()
    {
        return await _context.Gardens
            .Select(g => new GardenDto(g.Id, g.Name, g.Location, g.SizeSquareMeters, g.SoilType, g.SunExposure))
            .ToListAsync();
    }

    public async Task<GardenDto> CreateGardenAsync(CreateGardenRequest request)
    {
        var garden = new Garden
        {
            Name = request.Name,
            Location = request.Location,
            SizeSquareMeters = request.SizeSquareMeters,
            SoilType = request.SoilType,
            SunExposure = request.SunExposure
        };

        _context.Gardens.Add(garden);
        await _context.SaveChangesAsync();

        return new GardenDto(garden.Id, garden.Name, garden.Location, garden.SizeSquareMeters, garden.SoilType, garden.SunExposure);
    }

    public async Task<GardenDto?> GetGardenByIdAsync(int id)
    {
        var garden = await _context.Gardens.FindAsync(id);
        if (garden == null) return null;

        return new GardenDto(garden.Id, garden.Name, garden.Location, garden.SizeSquareMeters, garden.SoilType, garden.SunExposure);
    }
}
