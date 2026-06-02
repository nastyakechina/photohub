using Microsoft.EntityFrameworkCore;
using PhotoHub.FeedService.Domain.FeedItems;

namespace PhotoHub.FeedService.Infrastructure.Persistence;

public sealed class FeedDbContext(DbContextOptions<FeedDbContext> options) : DbContext(options)
{
    public DbSet<FeedItem> FeedItems => Set<FeedItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FeedItem>(entity =>
        {
            entity.ToTable("feed_items");

            entity.HasKey(fi => fi.Id);

            entity.Property(fi => fi.UserId).IsRequired();
            entity.Property(fi => fi.PhotoId).IsRequired();
            entity.Property(fi => fi.AuthorUserId).IsRequired();
            entity.Property(fi => fi.AuthorName).IsRequired();
            entity.Property(fi => fi.Title).IsRequired();
            entity.Property(fi => fi.ObjectKey).IsRequired();
            entity.Property(fi => fi.CreatedAtUtc).IsRequired();
            entity.Property(fi => fi.AddedToFeedAtUtc)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            entity.HasIndex(fi => new { fi.UserId, fi.AddedToFeedAtUtc });
        });
    }
}
