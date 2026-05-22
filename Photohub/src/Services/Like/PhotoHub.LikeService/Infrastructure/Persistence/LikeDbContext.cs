using Microsoft.EntityFrameworkCore;
using PhotoHub.LikeService.Domain.Likes;

namespace PhotoHub.LikeService.Infrastructure.Persistence;

public sealed class LikeDbContext(DbContextOptions<LikeDbContext> options) : DbContext(options)
{
    public DbSet<Like> Likes => Set<Like>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Like>(entity =>
        {
            entity.ToTable("likes");

            entity.HasKey(like => like.Id);

            entity.HasIndex(like => new { like.PhotoId, like.UserId })
                .IsUnique();

            entity.Property(like => like.PhotoId)
                .IsRequired();

            entity.Property(like => like.UserId)
                .IsRequired();

            entity.Property(like => like.CreatedAtUtc)
                .IsRequired();
        });
    }
}
