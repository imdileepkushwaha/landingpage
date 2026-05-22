using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftflipSolutions.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDemoRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Message",
                table: "DemoRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Requirement",
                table: "DemoRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Message",
                table: "DemoRequests");

            migrationBuilder.DropColumn(
                name: "Requirement",
                table: "DemoRequests");
        }
    }
}
