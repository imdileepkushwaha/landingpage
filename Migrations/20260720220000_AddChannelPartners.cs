using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SoftflipSolutions.Data;

#nullable disable

namespace SoftflipSolutions.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260720220000_AddChannelPartners")]
    partial class AddChannelPartners
    {
    }

    public partial class AddChannelPartners : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChannelPartners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OwnerName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Gstin = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Mobile = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    LogoPath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    State = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    City = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Pincode = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelPartners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PartnerClients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChannelPartnerId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Mobile = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    WhatsApp = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    Requirement = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Budget = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerClients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartnerClients_ChannelPartners_ChannelPartnerId",
                        column: x => x.ChannelPartnerId,
                        principalTable: "ChannelPartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PartnerProposals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChannelPartnerId = table.Column<int>(type: "int", nullable: false),
                    PartnerClientId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TemplateKey = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerProposals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartnerProposals_ChannelPartners_ChannelPartnerId",
                        column: x => x.ChannelPartnerId,
                        principalTable: "ChannelPartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartnerProposals_PartnerClients_PartnerClientId",
                        column: x => x.PartnerClientId,
                        principalTable: "PartnerClients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelPartners_Email",
                table: "ChannelPartners",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartnerClients_ChannelPartnerId",
                table: "PartnerClients",
                column: "ChannelPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerProposals_ChannelPartnerId",
                table: "PartnerProposals",
                column: "ChannelPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerProposals_PartnerClientId",
                table: "PartnerProposals",
                column: "PartnerClientId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PartnerProposals");
            migrationBuilder.DropTable(name: "PartnerClients");
            migrationBuilder.DropTable(name: "ChannelPartners");
        }
    }
}
