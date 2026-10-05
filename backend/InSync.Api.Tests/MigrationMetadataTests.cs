using Microsoft.EntityFrameworkCore;
using Xunit;

public sealed class MigrationMetadataTests
{
    [Fact]
    public void Production_migrations_are_discoverable_in_order()
    {
        var options = new DbContextOptionsBuilder<InSyncDbContext>()
            .UseNpgsql("Host=localhost;Database=insync;Username=test;Password=test")
            .Options;
        using var db = new InSyncDbContext(options);
        Assert.Equal(new[]
        {
            "20261002194000_InitialPersistence",
            "20261002201000_AddMediaProviderMetadata",
            "20261005020000_ExpandSecureRoomCodes"
        }, db.Database.GetMigrations());
    }
}
