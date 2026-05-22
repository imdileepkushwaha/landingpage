using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftflipSolutions.Migrations
{
    /// <inheritdoc />
    public partial class AddCRMWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Enquiries",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "DemoRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "DemoRequestNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DemoRequestId = table.Column<int>(type: "int", nullable: false),
                    NoteText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoRequestNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemoRequestNotes_DemoRequests_DemoRequestId",
                        column: x => x.DemoRequestId,
                        principalTable: "DemoRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EnquiryNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnquiryId = table.Column<int>(type: "int", nullable: false),
                    NoteText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnquiryNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnquiryNotes_Enquiries_EnquiryId",
                        column: x => x.EnquiryId,
                        principalTable: "Enquiries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DemoRequestNotes_DemoRequestId",
                table: "DemoRequestNotes",
                column: "DemoRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_EnquiryNotes_EnquiryId",
                table: "EnquiryNotes",
                column: "EnquiryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DemoRequestNotes");

            migrationBuilder.DropTable(
                name: "EnquiryNotes");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Enquiries");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "DemoRequests");
        }
    }
}
