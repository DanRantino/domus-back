using Domus.Domain.Houses;
using Domus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Domus.Infrastructure.DevelopmentSeed;

public sealed class HouseSeederDB
{
    private readonly DomusDbContext _dbContext;

    public HouseSeederDB(DomusDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<House>> RunAsync(
        CancellationToken cancellationToken = default)
    {
        var housesToSeed = SeedHouses.GetHouses();
        var houses = new List<House>();

        foreach (var seedHouse in housesToSeed)
        {
            var house = await _dbContext.Houses
                .FirstOrDefaultAsync(h => h.Name == seedHouse.name, cancellationToken);

            if (house is null)
            {
                house = new House
                {
                    Id = Guid.NewGuid(),
                    Name = seedHouse.name,
                };
                _dbContext.Houses.Add(house);
            }

            houses.Add(house);
        }

        await _dbContext.SaveIfChangedAsync(cancellationToken);

        return houses;
    }
}
