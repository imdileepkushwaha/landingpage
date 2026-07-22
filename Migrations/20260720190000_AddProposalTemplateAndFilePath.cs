using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftflipSolutions.Migrations
{
    public partial class AddProposalTemplateAndFilePath : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TemplateKey",
                table: "Proposals",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "classic");

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "Proposals",
                type: "nvarchar(260)",
                maxLength: 260,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "TemplateKey", table: "Proposals");
            migrationBuilder.DropColumn(name: "FilePath", table: "Proposals");
        }
    }
}
