using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Interfaces;

public interface IAppDbContext
{
    DbSet<Garden> Gardens { get; }
    DbSet<Plant> Plants { get; }
    DbSet<Planting> Plantings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
