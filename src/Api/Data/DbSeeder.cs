using Bogus;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        await context.Database.MigrateAsync();
        
        if (await context.Gardens.AnyAsync()) return;

        var faker = new Faker();

        var gardens = new Faker<Garden>()
            .RuleFor(g => g.Name, f => f.Address.City() + " Garden")
            .RuleFor(g => g.Location, f => f.Address.FullAddress())
            .RuleFor(g => g.SizeSquareMeters, f => f.Random.Double(10, 500))
            .RuleFor(g => g.SoilType, f => f.PickRandom<SoilType>())
            .RuleFor(g => g.SunExposure, f => f.PickRandom<SunExposure>())
            .Generate(100);

        context.Gardens.AddRange(gardens);
        await context.SaveChangesAsync();

        var plants = new Faker<Plant>()
            .RuleFor(p => p.Name, f => f.Commerce.ProductName())
            .RuleFor(p => p.Species, f => f.Hacker.Noun())
            .RuleFor(p => p.WaterFrequencyDays, f => f.Random.Int(1, 14))
            .RuleFor(p => p.SunRequirement, f => f.PickRandom<SunRequirement>())
            .RuleFor(p => p.GrowthDurationDays, f => f.Random.Int(30, 120))
            .RuleFor(p => p.PlantingSeason, f => f.PickRandom<PlantingSeason>())
            .RuleFor(p => p.SpaceRequiredSquareMeters, f => f.Random.Double(0.1, 5))
            .Generate(50);

        context.Plants.AddRange(plants);
        await context.SaveChangesAsync();

        var plantings = new List<Planting>();
        for (int i = 0; i < 9850; i++)
        {
            var garden = faker.PickRandom(gardens);
            var plant = faker.PickRandom(plants);

            // Try to pick a valid month for the plant's season
            int month = plant.PlantingSeason switch
            {
                PlantingSeason.Spring => faker.Random.Int(3, 5),
                PlantingSeason.Summer => faker.Random.Int(6, 8),
                PlantingSeason.Fall => faker.Random.Int(9, 11),
                PlantingSeason.Winter => faker.PickRandom(12, 1, 2),
                _ => 1
            };

            var plantedDate = new DateTime(2025, month, faker.Random.Int(1, 28), 0, 0, 0, DateTimeKind.Utc);

            plantings.Add(new Planting
            {
                GardenId = garden.Id,
                PlantId = plant.Id,
                PlantedDate = plantedDate,
                ExpectedHarvestDate = plantedDate.AddDays(plant.GrowthDurationDays),
                Status = faker.PickRandom<PlantingStatus>(),
                Notes = faker.Lorem.Sentence()
            });
        }

        context.Plantings.AddRange(plantings);
        await context.SaveChangesAsync();
    }
}
