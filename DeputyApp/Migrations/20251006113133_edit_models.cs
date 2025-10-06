using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeputyApp.Migrations
{
    /// <inheritdoc />
    public partial class edit_models : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "Catalogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Catalogs_OwnerId",
                table: "Catalogs",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Catalogs_Users_OwnerId",
                table: "Catalogs",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Catalogs_Users_OwnerId",
                table: "Catalogs");

            migrationBuilder.DropIndex(
                name: "IX_Catalogs_OwnerId",
                table: "Catalogs");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Catalogs");
        }
    }
}
