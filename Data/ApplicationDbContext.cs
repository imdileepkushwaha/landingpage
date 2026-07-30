using Microsoft.EntityFrameworkCore;
using SoftflipSolutions.Models;

namespace SoftflipSolutions.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Enquiry> Enquiries { get; set; }
    public DbSet<DemoRequest> DemoRequests { get; set; }
    public DbSet<EnquiryNote> EnquiryNotes { get; set; }
    public DbSet<DemoRequestNote> DemoRequestNotes { get; set; }
    public DbSet<ClientLead> ClientLeads { get; set; }
    public DbSet<ClientLeadNote> ClientLeadNotes { get; set; }
    public DbSet<Proposal> Proposals { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoicePayment> InvoicePayments { get; set; }
    public DbSet<LeadDocument> LeadDocuments { get; set; }
    public DbSet<FollowUpReminder> FollowUpReminders { get; set; }
    public DbSet<MessageTemplate> MessageTemplates { get; set; }
    public DbSet<ChannelPartner> ChannelPartners { get; set; }
    public DbSet<PartnerClient> PartnerClients { get; set; }
    public DbSet<PartnerProposal> PartnerProposals { get; set; }
    public DbSet<ServiceCatalog> ServiceCatalogs { get; set; }
    public DbSet<ServicePanel> ServicePanels { get; set; }
    public DbSet<ServiceModule> ServiceModules { get; set; }
    public DbSet<ServiceSubModule> ServiceSubModules { get; set; }
    public DbSet<AdminUser> AdminUsers { get; set; }
    public DbSet<AdminSetting> AdminSettings { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<AttendancePunch> AttendancePunches { get; set; }
    public DbSet<EmployeeDocumentTemplate> EmployeeDocumentTemplates { get; set; }
    public DbSet<EmployeeDocument> EmployeeDocuments { get; set; }
    public DbSet<EmployeeMenuPermission> EmployeeMenuPermissions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Invoice>()
            .HasOne(i => i.Proposal)
            .WithOne(p => p.Invoice)
            .HasForeignKey<Invoice>(i => i.ProposalId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Invoice>()
            .HasIndex(i => i.InvoiceNumber)
            .IsUnique();

        modelBuilder.Entity<InvoicePayment>()
            .HasOne(p => p.Invoice)
            .WithMany(i => i.Payments)
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LeadDocument>()
            .HasIndex(d => new { d.LeadType, d.LeadId });

        modelBuilder.Entity<FollowUpReminder>()
            .HasIndex(f => new { f.LeadType, f.LeadId, f.IsDone, f.DueAt });

        modelBuilder.Entity<ChannelPartner>()
            .HasIndex(p => p.Email)
            .IsUnique();

        modelBuilder.Entity<PartnerClient>()
            .HasOne(c => c.ChannelPartner)
            .WithMany(p => p.Clients)
            .HasForeignKey(c => c.ChannelPartnerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PartnerProposal>()
            .HasOne(p => p.ChannelPartner)
            .WithMany(c => c.Proposals)
            .HasForeignKey(p => p.ChannelPartnerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PartnerProposal>()
            .HasOne(p => p.PartnerClient)
            .WithMany(c => c.Proposals)
            .HasForeignKey(p => p.PartnerClientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PartnerProposal>()
            .HasOne(p => p.Service)
            .WithMany()
            .HasForeignKey(p => p.ServiceCatalogId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ServicePanel>()
            .HasOne(p => p.Service)
            .WithMany(s => s.Panels)
            .HasForeignKey(p => p.ServiceCatalogId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ServiceModule>()
            .HasOne(m => m.Panel)
            .WithMany(p => p.Modules)
            .HasForeignKey(m => m.ServicePanelId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ServiceSubModule>()
            .HasOne(s => s.Module)
            .WithMany(m => m.SubModules)
            .HasForeignKey(s => s.ServiceModuleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Proposal>()
            .HasOne(p => p.Service)
            .WithMany()
            .HasForeignKey(p => p.ServiceCatalogId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Employee>()
            .HasIndex(e => e.EmployeeCode)
            .IsUnique();

        modelBuilder.Entity<Employee>()
            .HasIndex(e => e.Email)
            .IsUnique();

        modelBuilder.Entity<AttendancePunch>()
            .HasOne(p => p.Employee)
            .WithMany(e => e.AttendancePunches)
            .HasForeignKey(p => p.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AttendancePunch>()
            .HasIndex(p => new { p.EmployeeId, p.PunchedAt });

        modelBuilder.Entity<EmployeeDocument>()
            .HasOne(d => d.Employee)
            .WithMany(e => e.Documents)
            .HasForeignKey(d => d.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EmployeeDocument>()
            .HasOne(d => d.Template)
            .WithMany(t => t.Documents)
            .HasForeignKey(d => d.TemplateId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<EmployeeMenuPermission>()
            .HasOne(p => p.Employee)
            .WithMany(e => e.MenuPermissions)
            .HasForeignKey(p => p.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EmployeeMenuPermission>()
            .HasIndex(p => new { p.EmployeeId, p.MenuKey })
            .IsUnique();

        modelBuilder.Entity<EmployeeDocumentTemplate>().HasData(
            new EmployeeDocumentTemplate
            {
                Id = 1,
                Name = "Offer Letter",
                DocumentType = "OfferLetter",
                Subject = "Offer of Employment - {{Designation}}",
                IsActive = true,
                IsSystem = true,
                CreatedAt = new DateTime(2026, 7, 30),
                Body = OfferLetterSeedBody
            },
            new EmployeeDocumentTemplate
            {
                Id = 2,
                Name = "Appointment Letter",
                DocumentType = "Appointment",
                Subject = "Appointment as {{Designation}}",
                IsActive = true,
                IsSystem = true,
                CreatedAt = new DateTime(2026, 7, 30),
                Body = HrmDocumentTemplateBodies.Appointment
            },
            new EmployeeDocumentTemplate
            {
                Id = 3,
                Name = "Experience Certificate",
                DocumentType = "Experience",
                Subject = "Experience Certificate - {{EmployeeName}}",
                IsActive = true,
                IsSystem = true,
                CreatedAt = new DateTime(2026, 7, 30),
                Body = HrmDocumentTemplateBodies.Experience
            },
            new EmployeeDocumentTemplate
            {
                Id = 4,
                Name = "Relieving Letter",
                DocumentType = "Relieving",
                Subject = "Relieving Letter - {{EmployeeName}}",
                IsActive = true,
                IsSystem = true,
                CreatedAt = new DateTime(2026, 7, 30),
                Body = HrmDocumentTemplateBodies.Relieving
            },
            new EmployeeDocumentTemplate
            {
                Id = 5,
                Name = "Warning Letter",
                DocumentType = "Warning",
                Subject = "Warning Letter - {{EmployeeName}}",
                IsActive = true,
                IsSystem = true,
                CreatedAt = new DateTime(2026, 7, 30),
                Body = HrmDocumentTemplateBodies.Warning
            });

        // Seed default admin user (password: admin123)
        // In a real app, use proper password hashing
        modelBuilder.Entity<AdminUser>().HasData(new AdminUser
        {
            Id = 1,
            Username = "admin",
            PasswordHash = "admin123"
        });
    }

    private const string OfferLetterSeedBody =
@"OFFER LETTER

Date: {{Date}}

To,
Mr./Ms. {{EmployeeName}}
{{Address}}

Subject: Offer of Employment - {{Designation}}

Dear {{EmployeeName}},

We are pleased to offer you the position of {{Designation}} at {{CompanyName}}, subject to the terms and conditions mentioned in this offer letter.

We believe that your skills, learning ability, and enthusiasm will contribute positively to our organization, and we look forward to having you as a part of our team.

1. Designation
Your designation will be:
{{Designation}}
You will work under the supervision of the Technical/Project Manager and will be responsible for supporting software development, IT coordination, testing, documentation, and day-to-day technical activities assigned by the management.

2. Date of Joining
Your expected date of joining will be:
{{JoiningDate}}
You are requested to report to the office at {{ReportingTime}} on your joining date.

3. Training / Probation Period
You will initially be appointed as a Trainee for a period of {{ProbationMonths}} months.
During the training period, your performance will be evaluated on the basis of:
Technical learning and programming skills
Software development work
Problem-solving ability
Project participation
IT coordination and support
Communication and teamwork
Punctuality and discipline
Ability to complete assigned tasks within deadlines
Based on your performance, you may be confirmed as a regular employee after successful completion of the training/probation period.

4. Compensation
During the training period, you will receive a stipend/salary of:
₹{{Amount}} per month
After successful completion of the training/probation period, your compensation may be revised based on your performance and company policy.
Any applicable statutory deductions will be made as per the applicable rules.

5. Job Responsibilities
As {{Designation}}, your responsibilities may include:

Software Development
Development and maintenance of web-based applications.
Coding, debugging, testing, and implementation of software modules.
Working with technologies such as HTML, CSS, Bootstrap, JavaScript, C#, ASP.NET and MS SQL Server, as required by projects.
Understanding project requirements and converting them into technical solutions.
Database development, queries, stored procedures, and data management.
Fixing bugs and resolving technical issues.
Maintaining proper coding and project documentation.
Participating in software testing and deployment.

IT Coordination
Coordinate with team members regarding project requirements and task status.
Assist in collecting and documenting client requirements.
Coordinate between development, testing, support, and management teams.
Maintain project/task status reports.
Provide basic technical support to internal users and clients when required.
Monitor assigned IT-related activities and ensure timely completion.
Assist in software installation, configuration, testing, and troubleshooting.

General Responsibilities
Follow company policies, processes, and instructions.
Maintain confidentiality of company and client information.
Maintain professional communication with clients and team members.
Complete assigned tasks within the agreed timeline.
Continuously improve technical and professional skills.

6. Working Hours
Your normal working hours will be:
{{WorkingHours}}
Working Days: {{WorkingDays}}
The working schedule may be changed according to project requirements and company policy.

7. Place of Work
Your primary place of work will be:
{{CompanyName}}
{{CompanyAddress}}
However, you may occasionally be required to visit client locations or other work locations for project-related activities, if required.

8. Leave and Holidays
Leave and holidays will be governed by the company's leave policy applicable to employees at your level and designation.
Prior approval from the reporting manager/management will be required for planned leave.

9. Confidentiality
During your employment, you may have access to confidential information relating to the company, clients, software source code, databases, credentials, business plans, pricing, documents, and other proprietary information.
You agree not to disclose, copy, transfer, misuse, or share such information with any unauthorized person during or after your employment.
All source code, project files, documents, databases, credentials, designs, and other work products created as part of your employment shall remain the property of {{CompanyName}} or the respective client, as applicable.

10. Company Assets
Any laptop, computer, software, documents, access credentials, ID card, storage devices, or other company property provided to you shall be used only for authorized business purposes.
All company property must be returned upon completion or termination of employment.

11. Performance and Confirmation
Your continuation and confirmation after the training/probation period will depend upon your overall performance, conduct, technical capabilities, attendance, discipline, teamwork, and business requirements of the company.
The company reserves the right to extend the training/probation period if required.

12. Termination / Notice Period
During the training/probation period, either party may terminate the employment by providing {{NoticeDays}} days' written notice, or salary in lieu of notice, subject to company policy and applicable law.
After confirmation, the applicable notice period will be as per the company's employment policy.
The company may take immediate disciplinary action in cases involving serious misconduct, unauthorized disclosure of confidential information, fraud, data misuse, or other serious violations, subject to applicable law.

13. Background Verification
Your appointment is subject to satisfactory verification of the information and documents provided by you, including educational qualifications, identity, previous employment details, and other information where applicable.
Providing false or misleading information may result in withdrawal of the offer or termination of employment.

14. Documents Required at Joining
You are required to submit copies of the following documents, as applicable:
Aadhaar Card / Valid ID Proof
PAN Card
Educational Qualification Certificates
Passport-size photographs
Bank Account Details
Address Proof
Previous Employment/Experience Certificate, if applicable
Other documents requested by the company

15. Acceptance of Offer
Please sign and return a copy of this offer letter as confirmation of your acceptance of the above terms and conditions.
We welcome you to {{CompanyName}} and hope that your association with us will provide you with valuable learning opportunities, professional growth, and a successful career.
We wish you all the best for your new role.

For {{CompanyName}}

Authorized Signatory
Name: {{SignatoryName}}
Designation: {{SignatoryTitle}}

ACCEPTANCE BY EMPLOYEE
I, {{EmployeeName}}, hereby accept the offer for the position of {{Designation}} at {{CompanyName}} and agree to abide by the terms and conditions mentioned in this offer letter and the applicable company policies.
Employee Name: __________________________
Signature: _______________________________
Date: ___________________________________
Place: ___________________________________";
}

