using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eatah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "diet_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_diet_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "meals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meals", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "weekly_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    week_number = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weekly_plans", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "diet_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    min_per_week = table.Column<int>(type: "integer", nullable: false),
                    max_per_week = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    diet_profile_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_diet_rules", x => x.id);
                    table.ForeignKey(
                        name: "FK_diet_rules_diet_profiles_diet_profile_id",
                        column: x => x.diet_profile_id,
                        principalTable: "diet_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ingredients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    meal_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ingredients", x => x.id);
                    table.ForeignKey(
                        name: "FK_ingredients_meals_meal_id",
                        column: x => x.meal_id,
                        principalTable: "meals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "day_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_of_week = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    meal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    weekly_plan_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_day_plans", x => x.id);
                    table.ForeignKey(
                        name: "FK_day_plans_meals_meal_id",
                        column: x => x.meal_id,
                        principalTable: "meals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_day_plans_weekly_plans_weekly_plan_id",
                        column: x => x.weekly_plan_id,
                        principalTable: "weekly_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_day_plans_meal_id",
                table: "day_plans",
                column: "meal_id");

            migrationBuilder.CreateIndex(
                name: "IX_day_plans_weekly_plan_id",
                table: "day_plans",
                column: "weekly_plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_diet_profiles_name",
                table: "diet_profiles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_diet_rules_diet_profile_id",
                table: "diet_rules",
                column: "diet_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_ingredients_meal_id",
                table: "ingredients",
                column: "meal_id");

            migrationBuilder.CreateIndex(
                name: "IX_meals_name",
                table: "meals",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_weekly_plans_year_week_number",
                table: "weekly_plans",
                columns: new[] { "year", "week_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "day_plans");

            migrationBuilder.DropTable(
                name: "diet_rules");

            migrationBuilder.DropTable(
                name: "ingredients");

            migrationBuilder.DropTable(
                name: "weekly_plans");

            migrationBuilder.DropTable(
                name: "diet_profiles");

            migrationBuilder.DropTable(
                name: "meals");
        }
    }
}
