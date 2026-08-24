using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftflipSolutions.Migrations
{
    [DbContext(typeof(SoftflipSolutions.Data.ApplicationDbContext))]
    [Migration("20260821123000_PartnerLoginPassword")]
    public partial class PartnerLoginPassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LoginPassword",
                table: "ChannelPartners",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE cp
SET cp.LoginPassword = cp.PasswordHash
FROM ChannelPartners cp
WHERE cp.LoginPassword IS NULL
  AND cp.PasswordHash IS NOT NULL
  AND cp.PasswordHash NOT LIKE '$2a$%'
  AND cp.PasswordHash NOT LIKE '$2b$%'
  AND cp.PasswordHash NOT LIKE '$2y$%'
  AND LEN(cp.PasswordHash) <= 100;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LoginPassword",
                table: "ChannelPartners");
        }
    }
}
