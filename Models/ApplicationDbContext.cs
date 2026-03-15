using Microsoft.EntityFrameworkCore;

namespace WealthTracker.Models;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Category> Categories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var prop = entity.FindProperty("RowVersion");
            if (prop != null && prop.ClrType == typeof(uint))
            {
                modelBuilder.Entity(entity.ClrType)
                    .Property("RowVersion")
                    .HasColumnName("xmin")
                    .HasColumnType("xid") 
                    .ValueGeneratedOnAddOrUpdate() 
                    .IsConcurrencyToken(); 
            }
        }
        
        modelBuilder.Entity<User>()
            .HasMany(u => u.Transactions) 
            .WithOne(t => t.User)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasMany(u => u.Categories)
            .WithOne(c => c.User)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is CommonData &&
                        (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                ((CommonData)entry.Entity).CreatedOn = DateTime.UtcNow;
            }

            ((CommonData)entry.Entity).UpdatedOn = DateTime.UtcNow;
        }
    }
}