using Microsoft.EntityFrameworkCore;
using TrueAltitude.Domain.Entities;

namespace TrueAltitude.Persistence.Data;

public class TrueAltitudeDbContext : DbContext
{
    public TrueAltitudeDbContext(DbContextOptions<TrueAltitudeDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User entity configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(500);

            entity.Property(e => e.Provider)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("local");

            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime(6)")
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

            entity.Property(e => e.OtpCode)
                .HasMaxLength(10);

            entity.Property(e => e.OtpExpiresAt)
                .HasColumnType("datetime(6)");

            entity.HasIndex(e => e.Email).IsUnique();
        });
    }
}
