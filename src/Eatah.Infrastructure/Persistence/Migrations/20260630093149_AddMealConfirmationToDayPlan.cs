using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eatah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMealConfirmationToDayPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "confirmation_status",
                table: "day_plans",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "confirmed_at",
                table: "day_plans",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "confirmation_status",
                table: "day_plans");

            migrationBuilder.DropColumn(
                name: "confirmed_at",
                table: "day_plans");
        }
    }
}
