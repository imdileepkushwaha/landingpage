using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftflipSolutions.Migrations
{
    [DbContext(typeof(SoftflipSolutions.Data.ApplicationDbContext))]
    [Migration("20260821140000_PartnerClientLeadAssignment")]
    public partial class PartnerClientLeadAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceLeadType",
                table: "PartnerClients",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceLeadId",
                table: "PartnerClients",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAt",
                table: "PartnerClients",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedBy",
                table: "PartnerClients",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignNote",
                table: "PartnerClients",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartnerClients_SourceLeadType_SourceLeadId",
                table: "PartnerClients",
                columns: new[] { "SourceLeadType", "SourceLeadId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PartnerClients_SourceLeadType_SourceLeadId",
                table: "PartnerClients");

            migrationBuilder.DropColumn(name: "SourceLeadType", table: "PartnerClients");
            migrationBuilder.DropColumn(name: "SourceLeadId", table: "PartnerClients");
            migrationBuilder.DropColumn(name: "AssignedAt", table: "PartnerClients");
            migrationBuilder.DropColumn(name: "AssignedBy", table: "PartnerClients");
            migrationBuilder.DropColumn(name: "AssignNote", table: "PartnerClients");
        }
    }
}
