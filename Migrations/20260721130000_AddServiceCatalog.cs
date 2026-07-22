using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftflipSolutions.Migrations
{
    public partial class AddServiceCatalog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SelectedModulesJson",
                table: "Proposals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServiceCatalogId",
                table: "Proposals",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ServiceCatalogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceCatalogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceModules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceCatalogId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceModules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceModules_ServiceCatalogs_ServiceCatalogId",
                        column: x => x.ServiceCatalogId,
                        principalTable: "ServiceCatalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceSubModules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceModuleId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceSubModules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceSubModules_ServiceModules_ServiceModuleId",
                        column: x => x.ServiceModuleId,
                        principalTable: "ServiceModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Proposals_ServiceCatalogId",
                table: "Proposals",
                column: "ServiceCatalogId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceModules_ServiceCatalogId",
                table: "ServiceModules",
                column: "ServiceCatalogId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceSubModules_ServiceModuleId",
                table: "ServiceSubModules",
                column: "ServiceModuleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Proposals_ServiceCatalogs_ServiceCatalogId",
                table: "Proposals",
                column: "ServiceCatalogId",
                principalTable: "ServiceCatalogs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Proposals_ServiceCatalogs_ServiceCatalogId",
                table: "Proposals");

            migrationBuilder.DropTable(
                name: "ServiceSubModules");

            migrationBuilder.DropTable(
                name: "ServiceModules");

            migrationBuilder.DropTable(
                name: "ServiceCatalogs");

            migrationBuilder.DropIndex(
                name: "IX_Proposals_ServiceCatalogId",
                table: "Proposals");

            migrationBuilder.DropColumn(
                name: "SelectedModulesJson",
                table: "Proposals");

            migrationBuilder.DropColumn(
                name: "ServiceCatalogId",
                table: "Proposals");
        }
    }
}
