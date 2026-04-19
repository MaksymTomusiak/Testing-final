using Domain.Enums;

namespace Application.Common.Dtos;

public record GardenDto(int Id, string Name, string Location, double SizeSquareMeters, SoilType SoilType, SunExposure SunExposure);

public record PlantDto(int Id, string Name, string Species, int WaterFrequencyDays, SunRequirement SunRequirement, int GrowthDurationDays, PlantingSeason PlantingSeason, double SpaceRequiredSquareMeters);

public record PlantingDto(int Id, int GardenId, int PlantId, DateTime PlantedDate, DateTime ExpectedHarvestDate, PlantingStatus Status, string Notes, PlantDto Plant);

public record CreateGardenRequest(string Name, string Location, double SizeSquareMeters, SoilType SoilType, SunExposure SunExposure);

public record CreatePlantRequest(string Name, string Species, int WaterFrequencyDays, SunRequirement SunRequirement, int GrowthDurationDays, PlantingSeason PlantingSeason, double SpaceRequiredSquareMeters);

public record CreatePlantingRequest(int PlantId, DateTime PlantedDate, string Notes);

public record UpdatePlantingStatusRequest(PlantingStatus Status);

public record WateringScheduleItem(DateTime Date, string PlantName, int PlantingId);

public record PaginatedList<T>(IEnumerable<T> Items, int TotalCount, int PageNumber, int PageSize, int TotalPages);
