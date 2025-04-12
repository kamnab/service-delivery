using Microsoft.EntityFrameworkCore;

internal class DatabaseSeeder
{
    private readonly ClientSideDbContext _dbContext;

    public DatabaseSeeder(ClientSideDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SeedAsync()
    {
        await _dbContext.Database.EnsureCreatedAsync();

        // Check if already seeded
        var seededSetting = await _dbContext.AppSettings
            .FirstOrDefaultAsync(s => s.Key == "IsSeeded");

        if (seededSetting?.Value == "true")
            return;

        // Seed initial data
        _dbContext.TodoItems.AddRange(
            new TodoItem { Name = "Wash the car", IsCompleted = false },
            new TodoItem { Name = "Write a Blazor app", IsCompleted = true },
            new TodoItem { Name = "Read EF Core docs", IsCompleted = false }
        );

        // Save items
        await _dbContext.SaveChangesAsync();

        // Mark as seeded
        _dbContext.AppSettings.Add(new AppSetting
        {
            Key = "IsSeeded",
            Value = "true"
        });

        await _dbContext.SaveChangesAsync();
    }
}
