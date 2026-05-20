using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eatah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CoveragePerDayPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Coverage semantics changed from per-Meal to per-DayPlan. Existing rows hold
            // MealId values that won't satisfy the new FK to day_plans, so wipe them first.
            migrationBuilder.Sql("DELETE FROM pantry_item_meal_coverages;");

            migrationBuilder.DropForeignKey(
                name: "FK_pantry_item_meal_coverages_meals_meal_id",
                table: "pantry_item_meal_coverages");

            migrationBuilder.RenameColumn(
                name: "meal_id",
                table: "pantry_item_meal_coverages",
                newName: "day_plan_id");

            migrationBuilder.RenameIndex(
                name: "IX_pantry_item_meal_coverages_pantry_item_id_meal_id",
                table: "pantry_item_meal_coverages",
                newName: "IX_pantry_item_meal_coverages_pantry_item_id_day_plan_id");

            migrationBuilder.RenameIndex(
                name: "IX_pantry_item_meal_coverages_meal_id",
                table: "pantry_item_meal_coverages",
                newName: "IX_pantry_item_meal_coverages_day_plan_id");

            migrationBuilder.AlterColumn<string>(
                name: "notes",
                table: "shopping_items",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_pantry_item_meal_coverages_day_plans_day_plan_id",
                table: "pantry_item_meal_coverages",
                column: "day_plan_id",
                principalTable: "day_plans",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_pantry_item_meal_coverages_day_plans_day_plan_id",
                table: "pantry_item_meal_coverages");

            migrationBuilder.RenameColumn(
                name: "day_plan_id",
                table: "pantry_item_meal_coverages",
                newName: "meal_id");

            migrationBuilder.RenameIndex(
                name: "IX_pantry_item_meal_coverages_pantry_item_id_day_plan_id",
                table: "pantry_item_meal_coverages",
                newName: "IX_pantry_item_meal_coverages_pantry_item_id_meal_id");

            migrationBuilder.RenameIndex(
                name: "IX_pantry_item_meal_coverages_day_plan_id",
                table: "pantry_item_meal_coverages",
                newName: "IX_pantry_item_meal_coverages_meal_id");

            migrationBuilder.AlterColumn<string>(
                name: "notes",
                table: "shopping_items",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_pantry_item_meal_coverages_meals_meal_id",
                table: "pantry_item_meal_coverages",
                column: "meal_id",
                principalTable: "meals",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
