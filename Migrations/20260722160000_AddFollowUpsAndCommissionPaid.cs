using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SoftflipSolutions.Data;

#nullable disable

namespace SoftflipSolutions.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260722160000_AddFollowUpsAndCommissionPaid")]
    partial class AddFollowUpsAndCommissionPaid
    {
    }

    public partial class AddFollowUpsAndCommissionPaid : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCommissionPaid",
                table: "PartnerProposals",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CommissionPaidAt",
                table: "PartnerProposals",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FollowUpReminders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeadType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    LeadId = table.Column<int>(type: "int", nullable: false),
                    DueAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsDone = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FollowUpReminders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FollowUpReminders_LeadType_LeadId_IsDone_DueAt",
                table: "FollowUpReminders",
                columns: new[] { "LeadType", "LeadId", "IsDone", "DueAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "FollowUpReminders");

            migrationBuilder.DropColumn(
                name: "IsCommissionPaid",
                table: "PartnerProposals");

            migrationBuilder.DropColumn(
                name: "CommissionPaidAt",
                table: "PartnerProposals");
        }
    }
}
