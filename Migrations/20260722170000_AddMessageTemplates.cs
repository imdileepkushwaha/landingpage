using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SoftflipSolutions.Data;

#nullable disable

namespace SoftflipSolutions.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260722170000_AddMessageTemplates")]
    partial class AddMessageTemplates
    {
    }

    public partial class AddMessageTemplates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MessageTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageTemplates", x => x.Id);
                });

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM MessageTemplates)
BEGIN
    SET IDENTITY_INSERT MessageTemplates ON;
    INSERT INTO MessageTemplates (Id, Name, Channel, Subject, Body, IsActive, CreatedAt, UpdatedAt) VALUES
    (1, N'Follow-up call', N'WhatsApp', NULL,
     N'Hi {{Name}},' + CHAR(10) + CHAR(10) + N'This is Softflip Solutions. Just following up on your interest in {{Requirement}}.' + CHAR(10) + CHAR(10) + N'When would be a good time for a quick call?' + CHAR(10) + CHAR(10) + N'Thanks,' + CHAR(10) + N'Softflip Team',
     1, '2026-07-22', NULL),
    (2, N'Proposal shared', N'WhatsApp', NULL,
     N'Hi {{Name}},' + CHAR(10) + CHAR(10) + N'Please find our proposal for {{Requirement}}. Happy to walk you through it on a call.' + CHAR(10) + CHAR(10) + N'Regards,' + CHAR(10) + N'Softflip Solutions',
     1, '2026-07-22', NULL),
    (3, N'Payment reminder', N'Email', N'Payment reminder — Softflip Solutions',
     N'Hi {{Name}},' + CHAR(10) + CHAR(10) + N'This is a friendly reminder regarding the pending payment for your project ({{Requirement}}).' + CHAR(10) + CHAR(10) + N'Please let us know if you need the invoice again or have any questions.' + CHAR(10) + CHAR(10) + N'Thank you,' + CHAR(10) + N'Softflip Solutions',
     1, '2026-07-22', NULL),
    (4, N'Thank you / next steps', N'Email', N'Thanks for connecting — Softflip Solutions',
     N'Hi {{Name}},' + CHAR(10) + CHAR(10) + N'Thank you for your interest in Softflip Solutions regarding {{Requirement}}.' + CHAR(10) + CHAR(10) + N'We''ll share the next steps shortly. Feel free to reply to this email with any questions.' + CHAR(10) + CHAR(10) + N'Best regards,' + CHAR(10) + N'Softflip Team',
     1, '2026-07-22', NULL);
    SET IDENTITY_INSERT MessageTemplates OFF;
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MessageTemplates");
        }
    }
}
