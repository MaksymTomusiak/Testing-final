using Domain.Enums;

namespace Domain.Entities;

public class Plant
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Species { get; set; } = string.Empty;
    public int WaterFrequencyDays { get; set; }
    public SunRequirement SunRequirement { get; set; }
    public int GrowthDurationDays { get; set; }
    public PlantingSeason PlantingSeason { get; set; }
    public double SpaceRequiredSquareMeters { get; set; } = 1.0; // Default space
}
