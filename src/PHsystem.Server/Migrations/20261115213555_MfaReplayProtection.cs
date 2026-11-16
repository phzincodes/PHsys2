using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PHsystem.Server.Migrations
{
    /// <inheritdoc />
    public partial class MfaReplayProtection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MfaUses",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Digest = table.Column<string>(type: "text", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MfaUses", x => new { x.UserId, x.Digest });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MfaUses");
        }
    }
}

