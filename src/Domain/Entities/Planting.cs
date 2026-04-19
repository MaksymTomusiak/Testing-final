using Domain.Enums;

namespace Domain.Entities;

public class Planting
{
    public int Id { get; set; }
    public int GardenId { get; set; }
    public int PlantId { get; set; }
    public DateTime PlantedDate { get; set; }
    public DateTime ExpectedHarvestDate { get; set; }
    public PlantingStatus Status { get; set; }
    public string Notes { get; set; } = string.Empty;

    public Garden Garden { get; set; } = null!;
    public Plant Plant { get; set; } = null!;
}
