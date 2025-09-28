using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gml.Web.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPermissionIsSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSystem",
                table: "Permissions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSystem",
                table: "Permissions");
        }
    }
}
