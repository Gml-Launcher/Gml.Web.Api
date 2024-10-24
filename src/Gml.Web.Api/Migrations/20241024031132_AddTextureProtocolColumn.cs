using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gml.Web.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTextureProtocolColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TextureProtocol",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TextureProtocol",
                table: "Settings");
        }
    }
}
