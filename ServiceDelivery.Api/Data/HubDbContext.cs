using Microsoft.EntityFrameworkCore;

public class HubDbContext : DbContext
{
    public HubDbContext(DbContextOptions<HubDbContext> options)
        : base(options)
    {

    }

    // Define your tables/entities here
    public DbSet<LicenseEntry> LicenseEntries { get; set; }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        //Configure default schema
        base.OnModelCreating(builder);

        builder.HasDefaultSchema(EFSettings.DB_SCHEME);

    }
}