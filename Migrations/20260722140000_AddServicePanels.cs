using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftflipSolutions.Migrations
{
    public partial class AddServicePanels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Clean partial apply from a previous failed run
            migrationBuilder.Sql(@"
                IF COL_LENGTH('ServiceModules', 'ServicePanelId') IS NOT NULL
                    ALTER TABLE ServiceModules DROP COLUMN ServicePanelId;

                IF OBJECT_ID(N'[ServicePanels]', N'U') IS NOT NULL
                    DROP TABLE [ServicePanels];
            ");

            migrationBuilder.CreateTable(
                name: "ServicePanels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceCatalogId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePanels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServicePanels_ServiceCatalogs_ServiceCatalogId",
                        column: x => x.ServiceCatalogId,
                        principalTable: "ServiceCatalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServicePanels_ServiceCatalogId",
                table: "ServicePanels",
                column: "ServiceCatalogId");

            migrationBuilder.Sql(@"
                INSERT INTO ServicePanels (ServiceCatalogId, Name, SortOrder)
                SELECT DISTINCT ServiceCatalogId, N'General', 0
                FROM ServiceModules;
            ");

            migrationBuilder.Sql(@"
                INSERT INTO ServicePanels (ServiceCatalogId, Name, SortOrder)
                SELECT c.Id, v.Name, v.SortOrder
                FROM ServiceCatalogs c
                CROSS JOIN (VALUES
                    (N'Admin Panel', 0),
                    (N'User Panel', 1),
                    (N'Franchise Panel', 2)
                ) AS v(Name, SortOrder)
                WHERE NOT EXISTS (
                    SELECT 1 FROM ServicePanels p WHERE p.ServiceCatalogId = c.Id
                );
            ");

            migrationBuilder.AddColumn<int>(
                name: "ServicePanelId",
                table: "ServiceModules",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE m
                SET m.ServicePanelId = p.Id
                FROM ServiceModules m
                INNER JOIN ServicePanels p ON p.ServiceCatalogId = m.ServiceCatalogId AND p.Name = N'General';
            ");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceModules_ServiceCatalogs_ServiceCatalogId",
                table: "ServiceModules");

            migrationBuilder.DropIndex(
                name: "IX_ServiceModules_ServiceCatalogId",
                table: "ServiceModules");

            migrationBuilder.DropColumn(
                name: "ServiceCatalogId",
                table: "ServiceModules");

            migrationBuilder.AlterColumn<int>(
                name: "ServicePanelId",
                table: "ServiceModules",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceModules_ServicePanelId",
                table: "ServiceModules",
                column: "ServicePanelId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceModules_ServicePanels_ServicePanelId",
                table: "ServiceModules",
                column: "ServicePanelId",
                principalTable: "ServicePanels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ServiceCatalogId",
                table: "ServiceModules",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE m
                SET m.ServiceCatalogId = p.ServiceCatalogId
                FROM ServiceModules m
                INNER JOIN ServicePanels p ON p.Id = m.ServicePanelId;
            ");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceModules_ServicePanels_ServicePanelId",
                table: "ServiceModules");

            migrationBuilder.DropIndex(
                name: "IX_ServiceModules_ServicePanelId",
                table: "ServiceModules");

            migrationBuilder.DropColumn(
                name: "ServicePanelId",
                table: "ServiceModules");

            migrationBuilder.AlterColumn<int>(
                name: "ServiceCatalogId",
                table: "ServiceModules",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceModules_ServiceCatalogId",
                table: "ServiceModules",
                column: "ServiceCatalogId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceModules_ServiceCatalogs_ServiceCatalogId",
                table: "ServiceModules",
                column: "ServiceCatalogId",
                principalTable: "ServiceCatalogs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropTable(
                name: "ServicePanels");
        }
    }
}
