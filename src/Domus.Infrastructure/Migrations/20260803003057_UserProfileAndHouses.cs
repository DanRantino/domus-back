using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domus.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UserProfileAndHouses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "full_name",
                table: "users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "notify_daily_tasks",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "notify_expenses",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "notify_family_chat",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "theme",
                table: "users",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "system");

            migrationBuilder.CreateTable(
                name: "houses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_houses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "house_memberships",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    house_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_house_memberships", x => new { x.user_id, x.house_id });
                    table.ForeignKey(
                        name: "FK_house_memberships_houses_house_id",
                        column: x => x.house_id,
                        principalTable: "houses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_house_memberships_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_house_memberships_house_id",
                table: "house_memberships",
                column: "house_id");

            migrationBuilder.CreateIndex(
                name: "IX_house_memberships_user_id_house_id",
                table: "house_memberships",
                columns: new[] { "user_id", "house_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "house_memberships");

            migrationBuilder.DropTable(
                name: "houses");

            migrationBuilder.DropColumn(
                name: "full_name",
                table: "users");

            migrationBuilder.DropColumn(
                name: "notify_daily_tasks",
                table: "users");

            migrationBuilder.DropColumn(
                name: "notify_expenses",
                table: "users");

            migrationBuilder.DropColumn(
                name: "notify_family_chat",
                table: "users");

            migrationBuilder.DropColumn(
                name: "theme",
                table: "users");
        }
    }
}
