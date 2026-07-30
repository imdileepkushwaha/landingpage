using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SoftflipSolutions.Migrations
{
    /// <inheritdoc />
    public partial class ImproveHrmDocumentUx : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DownloadedAt",
                table: "EmployeeDocuments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.InsertData(
                table: "EmployeeDocumentTemplates",
                columns: new[] { "Id", "Body", "CreatedAt", "DocumentType", "IsActive", "IsSystem", "Name", "Subject", "UpdatedAt" },
                values: new object[,]
                {
                    { 2, "APPOINTMENT LETTER\r\n\r\nDate: {{Date}}\r\n\r\nTo,\r\n{{EmployeeName}}\r\n{{Address}}\r\n\r\nSubject: Appointment as {{Designation}}\r\n\r\nDear {{EmployeeName}},\r\n\r\nWe are pleased to appoint you as {{Designation}} in the {{Department}} department of {{CompanyName}} with effect from {{JoiningDate}}.\r\n\r\n1. Designation & Department\r\nYou are appointed as {{Designation}} in {{Department}}.\r\n\r\n2. Compensation\r\nYour stipend/salary will be ₹{{Amount}} per month, subject to applicable statutory deductions.\r\n\r\n3. Working Hours\r\n{{WorkingHours}}\r\nWorking Days: {{WorkingDays}}\r\n\r\n4. Probation\r\nYou will be on probation for {{ProbationMonths}} months from the date of joining.\r\n\r\n5. Place of Work\r\n{{CompanyName}}\r\n{{CompanyAddress}}\r\n\r\n6. Notice Period\r\nEither party may terminate this appointment by giving {{NoticeDays}} days' written notice, or payment in lieu thereof, as per company policy.\r\n\r\nPlease report at {{ReportingTime}} on {{JoiningDate}}.\r\n\r\nWe welcome you to {{CompanyName}} and wish you a successful association.\r\n\r\nFor {{CompanyName}}\r\n{{SignatoryName}}\r\n{{SignatoryTitle}}", new DateTime(2026, 7, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "Appointment", true, true, "Appointment Letter", "Appointment as {{Designation}}", null },
                    { 3, "EXPERIENCE CERTIFICATE\r\n\r\nDate: {{Date}}\r\n\r\nTO WHOMSOEVER IT MAY CONCERN\r\n\r\nThis is to certify that {{EmployeeName}} (Employee Code: {{EmployeeCode}}) worked with {{CompanyName}} as {{Designation}} in the {{Department}} department.\r\n\r\nPeriod of employment: {{FromDate}} to {{ToDate}}\r\n\r\nDuring the tenure, {{EmployeeName}} performed duties related to the role of {{Designation}} and maintained professional conduct.\r\n\r\nWe wish {{EmployeeName}} success in future endeavors.\r\n\r\nFor {{CompanyName}}\r\n{{SignatoryName}}\r\n{{SignatoryTitle}}\r\n{{CompanyAddress}}\r\n{{CompanyPhone}} | {{CompanyEmail}}", new DateTime(2026, 7, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "Experience", true, true, "Experience Certificate", "Experience Certificate - {{EmployeeName}}", null },
                    { 4, "RELIEVING LETTER\r\n\r\nDate: {{Date}}\r\n\r\nTo,\r\n{{EmployeeName}}\r\n{{Address}}\r\n\r\nSubject: Relieving Letter\r\n\r\nDear {{EmployeeName}},\r\n\r\nThis is to confirm that you have been relieved from your duties as {{Designation}} at {{CompanyName}} with effect from {{LastWorkingDate}}.\r\n\r\nYour last working day with the organization was {{LastWorkingDate}}.\r\n\r\nAll company property, credentials, and documents entrusted to you must be returned (if not already done). Full and final settlement will be processed as per company policy.\r\n\r\nWe thank you for your association with {{CompanyName}} and wish you the best for the future.\r\n\r\nFor {{CompanyName}}\r\n{{SignatoryName}}\r\n{{SignatoryTitle}}", new DateTime(2026, 7, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "Relieving", true, true, "Relieving Letter", "Relieving Letter - {{EmployeeName}}", null },
                    { 5, "WARNING LETTER\r\n\r\nDate: {{Date}}\r\n\r\nTo,\r\n{{EmployeeName}}\r\n{{Designation}} · {{Department}}\r\nEmployee Code: {{EmployeeCode}}\r\n\r\nSubject: Warning Letter\r\n\r\nDear {{EmployeeName}},\r\n\r\nThis letter serves as a formal warning regarding the following matter:\r\n\r\n{{Reason}}\r\n\r\nYou are advised to improve and adhere to company policies, discipline, and performance expectations. Any further occurrence of a similar nature may lead to stricter disciplinary action, including termination, as per company policy.\r\n\r\nPlease treat this matter with seriousness.\r\n\r\nFor {{CompanyName}}\r\n{{SignatoryName}}\r\n{{SignatoryTitle}}\r\n\r\nAcknowledgement by Employee\r\nI have read and understood this warning letter.\r\nName: ________________ Signature: ________________ Date: ________________", new DateTime(2026, 7, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "Warning", true, true, "Warning Letter", "Warning Letter - {{EmployeeName}}", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "EmployeeDocumentTemplates",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "EmployeeDocumentTemplates",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "EmployeeDocumentTemplates",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "EmployeeDocumentTemplates",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DropColumn(
                name: "DownloadedAt",
                table: "EmployeeDocuments");
        }
    }
}
