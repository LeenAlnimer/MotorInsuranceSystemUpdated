using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotorInsurance.API.Migrations
{
    /// <inheritdoc />
    public partial class RefactorQuoteStatusAndPolicyStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "Quotes");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "Quotes",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Quotes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Policies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Policies");

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "Quotes",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
