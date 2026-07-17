using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace smartFollowup.API.Migrations
{
    /// <inheritdoc />
    public partial class AddOtpColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ResetToken",
                table: "Users",
                newName: "OtpCode");

            migrationBuilder.AddColumn<DateTime>(
                name: "OtpExpiry",
                table: "Users",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OtpExpiry",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "OtpCode",
                table: "Users",
                newName: "ResetToken");
        }
    }
}
