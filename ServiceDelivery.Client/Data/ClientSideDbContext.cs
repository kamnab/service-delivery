using Microsoft.EntityFrameworkCore;

internal class ClientSideDbContext : DbContext
{

    public ClientSideDbContext(DbContextOptions<ClientSideDbContext> options)
        : base(options)
    {
        // Configure the query tracking behavior globally
        // When this property is set to NoTracking, all queries will return
        // entities that are not tracked by the DbContext
        // However, we can still use individual query to override this behavior
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<TodoItem> TodoItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
