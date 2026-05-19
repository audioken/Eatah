using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eatah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPantryItemMealCoverage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pantry_item_meal_coverages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pantry_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    meal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    covers = table.Column<bool>(type: "boolean", nullable: false),
                    answered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pantry_item_meal_coverages", x => x.id);
                    table.ForeignKey(
                        name: "FK_pantry_item_meal_coverages_meals_meal_id",
                        column: x => x.meal_id,
                        principalTable: "meals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_pantry_item_meal_coverages_pantry_items_pantry_item_id",
                        column: x => x.pantry_item_id,
                        principalTable: "pantry_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pantry_item_meal_coverages_meal_id",
                table: "pantry_item_meal_coverages",
                column: "meal_id");

            migrationBuilder.CreateIndex(
                name: "IX_pantry_item_meal_coverages_pantry_item_id_meal_id",
                table: "pantry_item_meal_coverages",
                columns: new[] { "pantry_item_id", "meal_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pantry_item_meal_coverages");
        }
    }
}
