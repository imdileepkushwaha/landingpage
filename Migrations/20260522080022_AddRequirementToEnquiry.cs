using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftflipSolutions.Migrations
{
    /// <inheritdoc />
    public partial class AddRequirementToEnquiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Requirement",
                table: "Enquiries",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Requirement",
                table: "Enquiries");
        }
    }
}
