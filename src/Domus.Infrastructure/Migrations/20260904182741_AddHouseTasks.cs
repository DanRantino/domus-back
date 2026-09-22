using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domus.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHouseTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "house_tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    house_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    due_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    assignee_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_house_tasks", x => x.id);
                    table.ForeignKey(
                        name: "FK_house_tasks_house_memberships_assignee_user_id_house_id",
                        columns: x => new { x.assignee_user_id, x.house_id },
                        principalTable: "house_memberships",
                        principalColumns: new[] { "user_id", "house_id" });
                    table.ForeignKey(
                        name: "FK_house_tasks_house_memberships_created_by_user_id_house_id",
                        columns: x => new { x.created_by_user_id, x.house_id },
                        principalTable: "house_memberships",
                        principalColumns: new[] { "user_id", "house_id" });
                    table.ForeignKey(
                        name: "FK_house_tasks_houses_house_id",
                        column: x => x.house_id,
                        principalTable: "houses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_house_tasks_assignee_user_id_house_id",
                table: "house_tasks",
                columns: new[] { "assignee_user_id", "house_id" });

            migrationBuilder.CreateIndex(
                name: "IX_house_tasks_assignee_user_id_status",
                table: "house_tasks",
                columns: new[] { "assignee_user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_house_tasks_created_by_user_id_house_id",
                table: "house_tasks",
                columns: new[] { "created_by_user_id", "house_id" });

            migrationBuilder.CreateIndex(
                name: "IX_house_tasks_house_id_due_at",
                table: "house_tasks",
                columns: new[] { "house_id", "due_at" });

            migrationBuilder.CreateIndex(
                name: "IX_house_tasks_house_id_status",
                table: "house_tasks",
                columns: new[] { "house_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "house_tasks");
        }
    }
}
