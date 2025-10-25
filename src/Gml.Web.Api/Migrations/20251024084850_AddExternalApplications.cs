using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gml.Web.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalApplications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExternalApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    TokenHash = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalApplications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationPermissions",
                columns: table => new
                {
                    ApplicationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PermissionId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationPermissions", x => new { x.ApplicationId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_ApplicationPermissions_ExternalApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "ExternalApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicationPermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationPermissions_PermissionId",
                table: "ApplicationPermissions",
                column: "PermissionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationPermissions");

            migrationBuilder.DropTable(
                name: "ExternalApplications");
        }
    }
}
