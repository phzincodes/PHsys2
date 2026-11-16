using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PHsystem.Server.Migrations
{
    /// <inheritdoc />
    public partial class CommunityProfilesAndModeration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Posts_AuthorId",
                table: "Posts");

            migrationBuilder.AddColumn<string>(
                name: "Accent",
                table: "SocialProfiles",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#FF3347");

            migrationBuilder.AddColumn<bool>(
                name: "PublicVisible",
                table: "SocialProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Resolution",
                table: "Reports",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "pending");

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAt",
                table: "Reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Replies",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "ClientRequestId",
                table: "Posts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "Posts",
                type: "character varying(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "discussion");

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Posts",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "PostRevisions",
                type: "character varying(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "discussion");

            migrationBuilder.CreateTable(
                name: "FollowedPosts",
                columns: table => new
                {
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FollowedPosts", x => new { x.PostId, x.UserId });
                    table.ForeignKey(
                        name: "FK_FollowedPosts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FollowedPosts_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PublicHealthShares",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Mode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: true),
                    Band = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Lower = table.Column<int>(type: "integer", nullable: true),
                    Upper = table.Column<int>(type: "integer", nullable: true),
                    Coverage = table.Column<int>(type: "integer", nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IncludeRanking = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicHealthShares", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_PublicHealthShares_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PublicHealthShares_SocialProfiles_UserId",
                        column: x => x.UserId,
                        principalTable: "SocialProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReplyRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReplyId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplyRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReplyRevisions_Replies_ReplyId",
                        column: x => x.ReplyId,
                        principalTable: "Replies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_AuthorId_ClientRequestId",
                table: "Posts",
                columns: new[] { "AuthorId", "ClientRequestId" },
                unique: true,
                filter: "\"ClientRequestId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FollowedPosts_UserId",
                table: "FollowedPosts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplyRevisions_ReplyId_CreatedAt",
                table: "ReplyRevisions",
                columns: new[] { "ReplyId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FollowedPosts");

            migrationBuilder.DropTable(
                name: "PublicHealthShares");

            migrationBuilder.DropTable(
                name: "ReplyRevisions");

            migrationBuilder.DropIndex(
                name: "IX_Posts_AuthorId_ClientRequestId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "Accent",
                table: "SocialProfiles");

            migrationBuilder.DropColumn(
                name: "PublicVisible",
                table: "SocialProfiles");

            migrationBuilder.DropColumn(
                name: "Resolution",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "ResolvedAt",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Replies");

            migrationBuilder.DropColumn(
                name: "ClientRequestId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "PostRevisions");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_AuthorId",
                table: "Posts",
                column: "AuthorId");
        }
    }
}
