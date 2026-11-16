using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PHsystem.Server.Migrations
{
    /// <inheritdoc />
    public partial class ThreadedReplies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentId",
                table: "Replies",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Replies_ParentId",
                table: "Replies",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Replies_Replies_ParentId",
                table: "Replies",
                column: "ParentId",
                principalTable: "Replies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Replies_Replies_ParentId",
                table: "Replies");

            migrationBuilder.DropIndex(
                name: "IX_Replies_ParentId",
                table: "Replies");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "Replies");
        }
    }
}
