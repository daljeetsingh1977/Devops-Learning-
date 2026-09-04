using Kpmg.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Kpmg.Web.Data;

public class ComplaintDbContext : DbContext
{
    public ComplaintDbContext(DbContextOptions<ComplaintDbContext> options) : base(options)
    {
    }

    public DbSet<Complaint> Complaints => Set<Complaint>();

    public DbSet<ComplaintPhoto> ComplaintPhotos => Set<ComplaintPhoto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Complaint>(entity =>
        {
            entity.HasIndex(c => c.RequestNumber).IsUnique();
            entity.HasMany(c => c.Photos)
                .WithOne(p => p.Complaint!)
                .HasForeignKey(p => p.ComplaintId)
                .OnDelete(DeleteBehavior.Cascade);

            // SQLite cannot sort or compare DateTimeOffset values, so they are stored as UTC DateTime.
            entity.Property(c => c.CreatedAtUtc).HasConversion(UtcConverter);
            entity.Property(c => c.UpdatedAtUtc).HasConversion(UtcConverter);
        });
    }

    private static readonly ValueConverter<DateTimeOffset, DateTime> UtcConverter =
        new(value => value.UtcDateTime, value => new DateTimeOffset(value, TimeSpan.Zero));
}
