using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eatah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChatThreadReadStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chat_thread_read_statuses",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    thread_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_thread_read_statuses", x => new { x.user_id, x.thread_id });
                });

            migrationBuilder.CreateIndex(
                name: "IX_chat_thread_read_statuses_thread_id",
                table: "chat_thread_read_statuses",
                column: "thread_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_thread_read_statuses");
        }
    }
}
