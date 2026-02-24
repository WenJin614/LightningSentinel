using Microsoft.EntityFrameworkCore;

public class SentinelDbContext : DbContext
{
    public SentinelDbContext(DbContextOptions<SentinelDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Get all types in your project
        var entityTypes = typeof(SentinelDbContext).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Entity"));
        // Tip: I filtered by "Entity" suffix to avoid registering random helper classes

        foreach (var type in entityTypes)
        {
            // 2. Register each one with the ModelBuilder
            modelBuilder.Entity(type);
        }
    }
}