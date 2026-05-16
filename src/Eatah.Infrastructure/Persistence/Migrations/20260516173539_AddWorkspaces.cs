using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eatah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkspaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_weekly_plans_year_week_number",
                table: "weekly_plans");

            migrationBuilder.DropIndex(
                name: "IX_diet_profiles_name",
                table: "diet_profiles");

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "weekly_plans",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "meals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "diet_profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "workspaces",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workspaces", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workspace_members",
                columns: table => new
                {
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workspace_members", x => new { x.workspace_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_workspace_members_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_weekly_plans_workspace_id_year_week_number",
                table: "weekly_plans",
                columns: new[] { "workspace_id", "year", "week_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_meals_workspace_id",
                table: "meals",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_diet_profiles_workspace_id_name",
                table: "diet_profiles",
                columns: new[] { "workspace_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_workspace_members_user_id",
                table: "workspace_members",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workspace_members");

            migrationBuilder.DropTable(
                name: "workspaces");

            migrationBuilder.DropIndex(
                name: "IX_weekly_plans_workspace_id_year_week_number",
                table: "weekly_plans");

            migrationBuilder.DropIndex(
                name: "IX_meals_workspace_id",
                table: "meals");

            migrationBuilder.DropIndex(
                name: "IX_diet_profiles_workspace_id_name",
                table: "diet_profiles");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "weekly_plans");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "meals");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "diet_profiles");

            migrationBuilder.CreateIndex(
                name: "IX_weekly_plans_year_week_number",
                table: "weekly_plans",
                columns: new[] { "year", "week_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_diet_profiles_name",
                table: "diet_profiles",
                column: "name",
                unique: true);
        }
    }
}
