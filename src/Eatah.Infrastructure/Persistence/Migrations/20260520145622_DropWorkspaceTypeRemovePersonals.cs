using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eatah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropWorkspaceTypeRemovePersonals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Wipe everything owned by Personal workspaces before the type column goes away.
            // Cascade-delete relationships within each tree (ingredients/meal, rules/profile,
            // day_plans/weekly_plan, chat children) are handled by the existing FK definitions.
            // Workspace → workspace-scoped tables is NOT a real FK, so we delete those rows manually.
            migrationBuilder.Sql(@"
                WITH personal AS (SELECT id FROM workspaces WHERE type = 'Personal')
                DELETE FROM shopping_items
                WHERE workspace_id IN (SELECT id FROM personal);
            ");

            migrationBuilder.Sql(@"
                WITH personal AS (SELECT id FROM workspaces WHERE type = 'Personal')
                DELETE FROM pantry_items
                WHERE workspace_id IN (SELECT id FROM personal);
            ");

            migrationBuilder.Sql(@"
                WITH personal AS (SELECT id FROM workspaces WHERE type = 'Personal')
                DELETE FROM weekly_plans
                WHERE workspace_id IN (SELECT id FROM personal);
            ");

            migrationBuilder.Sql(@"
                WITH personal AS (SELECT id FROM workspaces WHERE type = 'Personal')
                DELETE FROM chat_threads
                WHERE workspace_id IN (SELECT id FROM personal);
            ");

            migrationBuilder.Sql(@"
                WITH personal AS (SELECT id FROM workspaces WHERE type = 'Personal')
                DELETE FROM diet_profiles
                WHERE workspace_id IN (SELECT id FROM personal);
            ");

            migrationBuilder.Sql(@"
                WITH personal AS (SELECT id FROM workspaces WHERE type = 'Personal')
                DELETE FROM meals
                WHERE workspace_id IN (SELECT id FROM personal);
            ");

            migrationBuilder.Sql(@"
                WITH personal AS (SELECT id FROM workspaces WHERE type = 'Personal')
                DELETE FROM ingredient_master
                WHERE workspace_id IN (SELECT id FROM personal);
            ");

            // Friend requests should not reference Personal households (the workspace itself was
            // already required to be Household to send one) — clean up defensively just in case.
            migrationBuilder.Sql(@"
                WITH personal AS (SELECT id FROM workspaces WHERE type = 'Personal')
                DELETE FROM friend_requests
                WHERE household_workspace_id IN (SELECT id FROM personal);
            ");

            // workspace_members has ON DELETE CASCADE from workspaces, so deleting the workspace
            // rows takes care of memberships automatically.
            migrationBuilder.Sql(@"DELETE FROM workspaces WHERE type = 'Personal';");

            migrationBuilder.DropColumn(
                name: "type",
                table: "workspaces");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "type",
                table: "workspaces",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Household");
        }
    }
}
