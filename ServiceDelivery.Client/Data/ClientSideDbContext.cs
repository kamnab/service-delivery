using Microsoft.EntityFrameworkCore;

internal class ClientSideDbContext : DbContext
{

    public ClientSideDbContext(DbContextOptions<ClientSideDbContext> options)
        : base(options)
    {
    }

    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<TodoItem> TodoItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Seed data here
        modelBuilder.Entity<TodoItem>().HasData(
            new TodoItem { Id = 1, Name = "Wash the car", IsCompleted = false },
            new TodoItem { Id = 2, Name = "Write a Blazor app", IsCompleted = true },
            new TodoItem { Id = 3, Name = "Read EF Core docs", IsCompleted = false }
        );
    }
}
