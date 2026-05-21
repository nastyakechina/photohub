using Microsoft.EntityFrameworkCore;
using PhotoHub.FriendsService.Domain.Follows;

namespace PhotoHub.FriendsService.Infrastructure.Persistence;

public sealed class FriendsDbContext(DbContextOptions<FriendsDbContext> options) : DbContext(options)
{
    public DbSet<Follow> Follows => Set<Follow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Follow>(entity =>
        {
            entity.ToTable("follows");

            entity.HasKey(follow => follow.Id);

            entity.HasIndex(follow => new { follow.FollowerId, follow.FollowingId })
                .IsUnique();

            entity.Property(follow => follow.FollowerId)
                .IsRequired();

            entity.Property(follow => follow.FollowingId)
                .IsRequired();

            entity.Property(follow => follow.CreatedAtUtc)
                .IsRequired();

            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "ck_follows_follower_id_not_equal_following_id",
                    "\"FollowerId\" <> \"FollowingId\"");
            });
        });
    }
}
