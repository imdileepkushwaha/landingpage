using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftflipSolutions.Migrations
{
    public partial class AddPartnerProposalServices : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SelectedModulesJson",
                table: "PartnerProposals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServiceCatalogId",
                table: "PartnerProposals",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartnerProposals_ServiceCatalogId",
                table: "PartnerProposals",
                column: "ServiceCatalogId");

            migrationBuilder.AddForeignKey(
                name: "FK_PartnerProposals_ServiceCatalogs_ServiceCatalogId",
                table: "PartnerProposals",
                column: "ServiceCatalogId",
                principalTable: "ServiceCatalogs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartnerProposals_ServiceCatalogs_ServiceCatalogId",
                table: "PartnerProposals");

            migrationBuilder.DropIndex(
                name: "IX_PartnerProposals_ServiceCatalogId",
                table: "PartnerProposals");

            migrationBuilder.DropColumn(
                name: "SelectedModulesJson",
                table: "PartnerProposals");

            migrationBuilder.DropColumn(
                name: "ServiceCatalogId",
                table: "PartnerProposals");
        }
    }
}
