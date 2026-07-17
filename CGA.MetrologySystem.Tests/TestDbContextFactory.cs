using CGA.MetrologySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CGA.MetrologySystem.Tests;

internal static class TestDbContextFactory
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"cga-tests-{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }
}
