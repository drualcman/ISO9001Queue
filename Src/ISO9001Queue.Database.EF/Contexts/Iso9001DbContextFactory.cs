namespace ISO9001Queue.Database.EF.Contexts;

internal sealed class Iso9001DbContextFactory : IDesignTimeDbContextFactory<Iso9001DbContext>
{
    public Iso9001DbContext CreateDbContext(string[] args)
    {
        var options = new DatabaseOptions
        {
            ConnectionString = "Server=(localdb)\\mssqllocaldb;Database=Iso9001Queue_Migrations;Trusted_Connection=True;"
        };
        return new Iso9001DbContext(new OptionsWrapper<DatabaseOptions>(options));
    }
}
