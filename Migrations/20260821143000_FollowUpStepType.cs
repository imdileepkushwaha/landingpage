using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftflipSolutions.Migrations
{
    [DbContext(typeof(SoftflipSolutions.Data.ApplicationDbContext))]
    [Migration("20260821143000_FollowUpStepType")]
    public partial class FollowUpStepType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StepType",
                table: "FollowUpReminders",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Note");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StepType",
                table: "FollowUpReminders");
        }
    }
}
