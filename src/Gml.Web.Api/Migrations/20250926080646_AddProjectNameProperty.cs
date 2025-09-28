using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gml.Web.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectNameProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProjectName",
                table: "Settings",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProjectName",
                table: "Settings");
        }
    }
}
