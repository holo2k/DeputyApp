using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DeputyId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_DeputyId",
                table: "Users",
                column: "DeputyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_DeputyId",
                table: "Users",
                column: "DeputyId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_DeputyId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_DeputyId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeputyId",
                table: "Users");
        }
    }
}
