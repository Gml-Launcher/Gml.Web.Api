using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gml.Web.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSentryAutoClearProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "SentryAutoClearPeriod",
                table: "Settings",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<bool>(
                name: "SentryNeedAutoClear",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SentryAutoClearPeriod",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "SentryNeedAutoClear",
                table: "Settings");
        }
    }
}
