using Microsoft.EntityFrameworkCore;
using PhotoHub.PhotoService.Domain.Photos;

namespace PhotoHub.PhotoService.Infrastructure.Persistence;

public sealed class PhotoDbContext(DbContextOptions<PhotoDbContext> options) : DbContext(options)
{
    public DbSet<Photo> Photos => Set<Photo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Photo>(entity =>
        {
            entity.ToTable("photos");

            entity.HasKey(photo => photo.Id);

            entity.HasIndex(photo => photo.AuthorUserId);

            entity.Property(photo => photo.AuthorUserId)
                .IsRequired();

            entity.Property(photo => photo.Title)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(photo => photo.Description)
                .HasMaxLength(2000);

            entity.Property(photo => photo.ObjectKey)
                .HasMaxLength(1024)
                .IsRequired();

            entity.Property(photo => photo.PreviewObjectKey)
                .HasMaxLength(1024);

            entity.Property(photo => photo.CreatedAtUtc)
                .IsRequired();
        });
    }
}
