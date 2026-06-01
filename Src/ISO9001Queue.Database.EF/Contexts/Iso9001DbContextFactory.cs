namespace ISO9001Queue.Database.EF.Contexts;

/// <summary>
/// Add-Migration AddSharedLinks -p ISO9001Queue.Database.EF -s ISO9001Queue.Database.EF -c Iso9001DbContext  -o Migrations
/// Update-Database -p ISO9001Queue.Database.EF -s ISO9001Queue.Database.EF -context Iso9001DbContext
/// </summary>
internal sealed class Iso9001DbContextFactory : IDesignTimeDbContextFactory<Iso9001DbContext>
{
    public Iso9001DbContext CreateDbContext(string[] args)
    {
        var options = new DatabaseOptions
        {
            ConnectionString = "Server=(localdb)\\mssqllocaldb;Database=Iso9001db;Trusted_Connection=True;"
        };
        return new Iso9001DbContext(new OptionsWrapper<DatabaseOptions>(options));
    }
}
