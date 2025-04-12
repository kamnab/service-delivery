using Microsoft.EntityFrameworkCore;

public static class ClientSideDataServices
{
    public static void AddClientSideDataDbContext(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddDbContextFactory<ClientSideDbContext>(
            options => options.UseSqlite($"Filename={DataSynchronizer.SqliteDbFilename}"));
        serviceCollection.AddScoped<DataSynchronizer>();
    }
}
