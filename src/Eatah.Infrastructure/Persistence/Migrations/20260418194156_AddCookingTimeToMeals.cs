using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eatah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCookingTimeToMeals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "cooking_time_minutes",
                table: "meals",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cooking_time_minutes",
                table: "meals");
        }
    }
}
