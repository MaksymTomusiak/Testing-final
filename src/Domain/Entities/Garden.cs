using Domain.Enums;

namespace Domain.Entities;

public class Garden
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public double SizeSquareMeters { get; set; }
    public SoilType SoilType { get; set; }
    public SunExposure SunExposure { get; set; }

    public ICollection<Planting> Plantings { get; set; } = new List<Planting>();
}
