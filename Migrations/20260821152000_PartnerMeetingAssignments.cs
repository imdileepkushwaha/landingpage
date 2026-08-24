using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftflipSolutions.Migrations
{
    [DbContext(typeof(SoftflipSolutions.Data.ApplicationDbContext))]
    [Migration("20260821152000_PartnerMeetingAssignments")]
    public partial class PartnerMeetingAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AssignToAllPartners",
                table: "PartnerMeetings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "PartnerMeetingAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartnerMeetingId = table.Column<int>(type: "int", nullable: false),
                    ChannelPartnerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerMeetingAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartnerMeetingAssignments_ChannelPartners_ChannelPartnerId",
                        column: x => x.ChannelPartnerId,
                        principalTable: "ChannelPartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PartnerMeetingAssignments_PartnerMeetings_PartnerMeetingId",
                        column: x => x.PartnerMeetingId,
                        principalTable: "PartnerMeetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PartnerMeetingAssignments_ChannelPartnerId",
                table: "PartnerMeetingAssignments",
                column: "ChannelPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerMeetingAssignments_PartnerMeetingId_ChannelPartnerId",
                table: "PartnerMeetingAssignments",
                columns: new[] { "PartnerMeetingId", "ChannelPartnerId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PartnerMeetingAssignments");
            migrationBuilder.DropColumn(name: "AssignToAllPartners", table: "PartnerMeetings");
        }
    }
}
