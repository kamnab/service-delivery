using Microsoft.EntityFrameworkCore;

public static class ClientSideDataServices
{
    public static void AddClientSideDataDbContext(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddDbContextFactory<ClientSideDbContext>(
            options =>
            {
                options.UseSqlite($"Filename={DataSynchronizer.SqliteDbFilename}");

                // Disable or reduce logging
                options.EnableSensitiveDataLogging(false);
                options.LogTo(_ => { }, LogLevel.None);
            });
        //serviceCollection.AddScoped<DataSynchronizer>();
    }
}
