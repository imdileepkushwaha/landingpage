using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftflipSolutions.Migrations
{
    [DbContext(typeof(SoftflipSolutions.Data.ApplicationDbContext))]
    [Migration("20260821183000_PartnerBankUpiDetails")]
    public partial class PartnerBankUpiDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "ChannelPartners",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankAccountName",
                table: "ChannelPartners",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankAccountNumber",
                table: "ChannelPartners",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankIfsc",
                table: "ChannelPartners",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankBranch",
                table: "ChannelPartners",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpiId",
                table: "ChannelPartners",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpiName",
                table: "ChannelPartners",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpiQrPath",
                table: "ChannelPartners",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "BankName", table: "ChannelPartners");
            migrationBuilder.DropColumn(name: "BankAccountName", table: "ChannelPartners");
            migrationBuilder.DropColumn(name: "BankAccountNumber", table: "ChannelPartners");
            migrationBuilder.DropColumn(name: "BankIfsc", table: "ChannelPartners");
            migrationBuilder.DropColumn(name: "BankBranch", table: "ChannelPartners");
            migrationBuilder.DropColumn(name: "UpiId", table: "ChannelPartners");
            migrationBuilder.DropColumn(name: "UpiName", table: "ChannelPartners");
            migrationBuilder.DropColumn(name: "UpiQrPath", table: "ChannelPartners");
        }
    }
}
