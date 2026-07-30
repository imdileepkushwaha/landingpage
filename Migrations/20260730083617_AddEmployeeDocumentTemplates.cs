using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftflipSolutions.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeDocumentTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployeeDocumentTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeDocumentTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    TemplateId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GeneratedBy = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentToEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ExtraFieldsJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeDocuments_EmployeeDocumentTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "EmployeeDocumentTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EmployeeDocuments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "EmployeeDocumentTemplates",
                columns: new[] { "Id", "Body", "CreatedAt", "DocumentType", "IsActive", "IsSystem", "Name", "Subject", "UpdatedAt" },
                values: new object[] { 1, "OFFER LETTER\r\n\r\nDate: {{Date}}\r\n\r\nTo,\r\nMr./Ms. {{EmployeeName}}\r\n{{Address}}\r\n\r\nSubject: Offer of Employment - {{Designation}}\r\n\r\nDear {{EmployeeName}},\r\n\r\nWe are pleased to offer you the position of {{Designation}} at {{CompanyName}}, subject to the terms and conditions mentioned in this offer letter.\r\n\r\nWe believe that your skills, learning ability, and enthusiasm will contribute positively to our organization, and we look forward to having you as a part of our team.\r\n\r\n1. Designation\r\nYour designation will be:\r\n{{Designation}}\r\nYou will work under the supervision of the Technical/Project Manager and will be responsible for supporting software development, IT coordination, testing, documentation, and day-to-day technical activities assigned by the management.\r\n\r\n2. Date of Joining\r\nYour expected date of joining will be:\r\n{{JoiningDate}}\r\nYou are requested to report to the office at {{ReportingTime}} on your joining date.\r\n\r\n3. Training / Probation Period\r\nYou will initially be appointed as a Trainee for a period of {{ProbationMonths}} months.\r\nDuring the training period, your performance will be evaluated on the basis of:\r\nTechnical learning and programming skills\r\nSoftware development work\r\nProblem-solving ability\r\nProject participation\r\nIT coordination and support\r\nCommunication and teamwork\r\nPunctuality and discipline\r\nAbility to complete assigned tasks within deadlines\r\nBased on your performance, you may be confirmed as a regular employee after successful completion of the training/probation period.\r\n\r\n4. Compensation\r\nDuring the training period, you will receive a stipend/salary of:\r\n₹{{Amount}} per month\r\nAfter successful completion of the training/probation period, your compensation may be revised based on your performance and company policy.\r\nAny applicable statutory deductions will be made as per the applicable rules.\r\n\r\n5. Job Responsibilities\r\nAs {{Designation}}, your responsibilities may include:\r\n\r\nSoftware Development\r\nDevelopment and maintenance of web-based applications.\r\nCoding, debugging, testing, and implementation of software modules.\r\nWorking with technologies such as HTML, CSS, Bootstrap, JavaScript, C#, ASP.NET and MS SQL Server, as required by projects.\r\nUnderstanding project requirements and converting them into technical solutions.\r\nDatabase development, queries, stored procedures, and data management.\r\nFixing bugs and resolving technical issues.\r\nMaintaining proper coding and project documentation.\r\nParticipating in software testing and deployment.\r\n\r\nIT Coordination\r\nCoordinate with team members regarding project requirements and task status.\r\nAssist in collecting and documenting client requirements.\r\nCoordinate between development, testing, support, and management teams.\r\nMaintain project/task status reports.\r\nProvide basic technical support to internal users and clients when required.\r\nMonitor assigned IT-related activities and ensure timely completion.\r\nAssist in software installation, configuration, testing, and troubleshooting.\r\n\r\nGeneral Responsibilities\r\nFollow company policies, processes, and instructions.\r\nMaintain confidentiality of company and client information.\r\nMaintain professional communication with clients and team members.\r\nComplete assigned tasks within the agreed timeline.\r\nContinuously improve technical and professional skills.\r\n\r\n6. Working Hours\r\nYour normal working hours will be:\r\n{{WorkingHours}}\r\nWorking Days: {{WorkingDays}}\r\nThe working schedule may be changed according to project requirements and company policy.\r\n\r\n7. Place of Work\r\nYour primary place of work will be:\r\n{{CompanyName}}\r\n{{CompanyAddress}}\r\nHowever, you may occasionally be required to visit client locations or other work locations for project-related activities, if required.\r\n\r\n8. Leave and Holidays\r\nLeave and holidays will be governed by the company's leave policy applicable to employees at your level and designation.\r\nPrior approval from the reporting manager/management will be required for planned leave.\r\n\r\n9. Confidentiality\r\nDuring your employment, you may have access to confidential information relating to the company, clients, software source code, databases, credentials, business plans, pricing, documents, and other proprietary information.\r\nYou agree not to disclose, copy, transfer, misuse, or share such information with any unauthorized person during or after your employment.\r\nAll source code, project files, documents, databases, credentials, designs, and other work products created as part of your employment shall remain the property of {{CompanyName}} or the respective client, as applicable.\r\n\r\n10. Company Assets\r\nAny laptop, computer, software, documents, access credentials, ID card, storage devices, or other company property provided to you shall be used only for authorized business purposes.\r\nAll company property must be returned upon completion or termination of employment.\r\n\r\n11. Performance and Confirmation\r\nYour continuation and confirmation after the training/probation period will depend upon your overall performance, conduct, technical capabilities, attendance, discipline, teamwork, and business requirements of the company.\r\nThe company reserves the right to extend the training/probation period if required.\r\n\r\n12. Termination / Notice Period\r\nDuring the training/probation period, either party may terminate the employment by providing {{NoticeDays}} days' written notice, or salary in lieu of notice, subject to company policy and applicable law.\r\nAfter confirmation, the applicable notice period will be as per the company's employment policy.\r\nThe company may take immediate disciplinary action in cases involving serious misconduct, unauthorized disclosure of confidential information, fraud, data misuse, or other serious violations, subject to applicable law.\r\n\r\n13. Background Verification\r\nYour appointment is subject to satisfactory verification of the information and documents provided by you, including educational qualifications, identity, previous employment details, and other information where applicable.\r\nProviding false or misleading information may result in withdrawal of the offer or termination of employment.\r\n\r\n14. Documents Required at Joining\r\nYou are required to submit copies of the following documents, as applicable:\r\nAadhaar Card / Valid ID Proof\r\nPAN Card\r\nEducational Qualification Certificates\r\nPassport-size photographs\r\nBank Account Details\r\nAddress Proof\r\nPrevious Employment/Experience Certificate, if applicable\r\nOther documents requested by the company\r\n\r\n15. Acceptance of Offer\r\nPlease sign and return a copy of this offer letter as confirmation of your acceptance of the above terms and conditions.\r\nWe welcome you to {{CompanyName}} and hope that your association with us will provide you with valuable learning opportunities, professional growth, and a successful career.\r\nWe wish you all the best for your new role.\r\n\r\nFor {{CompanyName}}\r\n\r\nAuthorized Signatory\r\nName: {{SignatoryName}}\r\nDesignation: {{SignatoryTitle}}\r\n\r\nACCEPTANCE BY EMPLOYEE\r\nI, {{EmployeeName}}, hereby accept the offer for the position of {{Designation}} at {{CompanyName}} and agree to abide by the terms and conditions mentioned in this offer letter and the applicable company policies.\r\nEmployee Name: __________________________\r\nSignature: _______________________________\r\nDate: ___________________________________\r\nPlace: ___________________________________", new DateTime(2026, 7, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "OfferLetter", true, true, "Offer Letter", "Offer of Employment - {{Designation}}", null });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocuments_EmployeeId",
                table: "EmployeeDocuments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocuments_TemplateId",
                table: "EmployeeDocuments",
                column: "TemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeDocuments");

            migrationBuilder.DropTable(
                name: "EmployeeDocumentTemplates");
        }
    }
}
