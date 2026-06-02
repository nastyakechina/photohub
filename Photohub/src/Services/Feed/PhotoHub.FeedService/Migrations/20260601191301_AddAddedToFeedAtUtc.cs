using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoHub.FeedService.Migrations
{
    /// <inheritdoc />
    public partial class AddAddedToFeedAtUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_feed_items_UserId_CreatedAtUtc",
                table: "feed_items");

            migrationBuilder.AddColumn<DateTime>(
                name: "AddedToFeedAtUtc",
                table: "feed_items",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.CreateIndex(
                name: "IX_feed_items_UserId_AddedToFeedAtUtc",
                table: "feed_items",
                columns: new[] { "UserId", "AddedToFeedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_feed_items_UserId_AddedToFeedAtUtc",
                table: "feed_items");

            migrationBuilder.DropColumn(
                name: "AddedToFeedAtUtc",
                table: "feed_items");

            migrationBuilder.CreateIndex(
                name: "IX_feed_items_UserId_CreatedAtUtc",
                table: "feed_items",
                columns: new[] { "UserId", "CreatedAtUtc" });
        }
    }
}
