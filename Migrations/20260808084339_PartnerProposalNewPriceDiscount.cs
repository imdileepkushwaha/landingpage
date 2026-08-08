using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftflipSolutions.Migrations
{
    /// <inheritdoc />
    public partial class PartnerProposalNewPriceDiscount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                table: "Proposals",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalAmount",
                table: "Proposals",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                table: "PartnerProposals",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalAmount",
                table: "PartnerProposals",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "Proposals");

            migrationBuilder.DropColumn(
                name: "OriginalAmount",
                table: "Proposals");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "PartnerProposals");

            migrationBuilder.DropColumn(
                name: "OriginalAmount",
                table: "PartnerProposals");
        }
    }
}
