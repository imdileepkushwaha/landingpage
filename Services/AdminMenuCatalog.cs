namespace SoftflipSolutions.Services;

public record AdminMenuItem(
    string Key,
    string Label,
    string Icon,
    string Section,
    string? Description = null);

/// <summary>Catalog of allotable admin sidebar menus (same idea as EmployeeMenuCatalog).</summary>
public static class AdminMenuCatalog
{
    public const string Dashboard = "dashboard";

    // CRM
    public const string Enquiries = "enquiries";
    public const string DemoRequests = "demos";
    public const string ClientLeads = "client_leads";
    public const string MergeLeads = "merge_leads";
    public const string Services = "services";
    public const string ChannelPartners = "partners";
    public const string PartnerClients = "partner_clients";
    public const string PartnerLeadTracking = "partner_lead_tracking";
    public const string PartnerMeetings = "partner_meetings";
    public const string PartnerTickets = "partner_tickets";
    public const string MarketingKit = "marketing_kit";

    // Sales
    public const string PipelineBoard = "pipeline_board";
    public const string ActiveDeals = "active_deals";
    public const string LeadTasks = "lead_tasks";
    public const string FollowUps = "followups";
    public const string FollowUpNudges = "followup_nudges";
    public const string Rejected = "rejected";

    // Billing
    public const string Proposals = "proposals";
    public const string Invoices = "invoices";
    public const string InvoiceReminders = "invoice_reminders";
    public const string RecurringInvoices = "recurring_invoices";

    // HRM
    public const string Employees = "employees";
    public const string Punch = "punch";
    public const string AttendanceReport = "attendance_report";
    public const string Leave = "leave";
    public const string Holidays = "holidays";
    public const string DocTemplates = "doc_templates";
    public const string BulkDocuments = "bulk_documents";
    public const string Payslips = "payslips";

    // Reports
    public const string SalesSummary = "sales_summary";
    public const string PartnerPerformance = "partner_performance";
    public const string Commission = "commission";
    public const string Activity = "activity";

    // System
    public const string Settings = "settings";
    public const string MessageTemplates = "message_templates";
    public const string AdminUsers = "admin_users";
    public const string Notifications = "notifications";
    public const string AuditLogs = "audit_logs";
    public const string EmailLogs = "email_logs";
    public const string DataExport = "data_export";
    public const string WhatsApp = "whatsapp";

    public static IReadOnlyList<AdminMenuItem> All { get; } =
    [
        new(Dashboard, "Dashboard", "bi-grid", "Overview", "Home overview"),

        new(Enquiries, "Enquiries", "bi-envelope", "CRM", "Website enquiries"),
        new(DemoRequests, "Demo Requests", "bi-laptop", "CRM", "Demo form leads"),
        new(ClientLeads, "External Clients", "bi-people", "CRM", "WhatsApp / Just Dial leads"),
        new(MergeLeads, "Merge Leads", "bi-intersect", "CRM", "Merge duplicate client leads"),
        new(Services, "Services", "bi-box-seam", "CRM", "Service catalog & modules"),
        new(ChannelPartners, "Channel Partners", "bi-building", "CRM", "Partner companies"),
        new(PartnerClients, "Partner Clients", "bi-people-fill", "CRM", "Clients from partners"),
        new(PartnerLeadTracking, "Partner Lead Tracking", "bi-signpost-2", "CRM", "Assigned leads & partner follow-ups"),
        new(PartnerMeetings, "Partner Meetings", "bi-camera-video", "CRM", "Meeting links for partners"),
        new(PartnerTickets, "Partner Tickets", "bi-life-preserver", "CRM", "Partner support tickets"),
        new(MarketingKit, "Marketing Kit", "bi-images", "CRM", "Assets for partner download"),

        new(PipelineBoard, "Pipeline Board", "bi-columns-gap", "Sales", "Kanban board"),
        new(ActiveDeals, "Active Deals", "bi-briefcase", "Sales", "Confirmed deals"),
        new(LeadTasks, "Lead Tasks", "bi-list-task", "Sales", "To-dos on leads"),
        new(FollowUps, "Follow-ups", "bi-alarm", "Sales", "Follow-up reminders"),
        new(FollowUpNudges, "Overdue Nudges", "bi-lightning", "Sales", "Overdue follow-up nudges"),
        new(Rejected, "Rejected", "bi-x-circle", "Sales", "Rejected leads"),

        new(Proposals, "Proposals", "bi-file-earmark-text", "Billing", "All proposals"),
        new(Invoices, "Invoices", "bi-receipt", "Billing", "All invoices"),
        new(InvoiceReminders, "Invoice Reminders", "bi-bell", "Billing", "Unpaid invoice nudges"),
        new(RecurringInvoices, "Recurring Invoices", "bi-arrow-repeat", "Billing", "AMC / recurring billing"),

        new(Employees, "Employees", "bi-people", "HRM", "Employee list & details"),
        new(Punch, "Punch Attendance", "bi-fingerprint", "HRM", "Check in / out"),
        new(AttendanceReport, "Attendance Report", "bi-table", "HRM", "Monthly attendance"),
        new(Leave, "Leave", "bi-calendar-x", "HRM", "Leave requests"),
        new(Holidays, "Holidays", "bi-calendar-event", "HRM", "Company holidays"),
        new(DocTemplates, "Document Templates", "bi-file-earmark-text", "HRM", "HR letter templates"),
        new(BulkDocuments, "Bulk Documents", "bi-files", "HRM", "Bulk generate letters"),
        new(Payslips, "Payslips", "bi-cash-stack", "HRM", "Salary & payslips"),

        new(SalesSummary, "Sales Summary", "bi-pie-chart", "Reports", "Sales by date range"),
        new(PartnerPerformance, "Partner Performance", "bi-graph-up-arrow", "Reports", "Partner metrics"),
        new(Commission, "Commission Tracker", "bi-percent", "Reports", "Partner commissions"),
        new(Activity, "Activity Timeline", "bi-activity", "Reports", "Recent activity"),

        new(Settings, "Settings", "bi-sliders", "System", "Company & SMTP"),
        new(MessageTemplates, "Message Templates", "bi-chat-quote", "System", "Email / WhatsApp templates"),
        new(AdminUsers, "Admin Users", "bi-shield-lock", "System", "Users & menu access"),
        new(Notifications, "Notifications", "bi-bell-fill", "System", "Admin alerts"),
        new(AuditLogs, "Audit Log", "bi-journal-text", "System", "Who changed what"),
        new(EmailLogs, "Email Log", "bi-envelope-paper", "System", "Sent email history"),
        new(DataExport, "Data Export", "bi-download", "System", "CSV exports"),
        new(WhatsApp, "WhatsApp API", "bi-whatsapp", "System", "WhatsApp Business settings")
    ];

    public static IReadOnlyList<string> AllKeys { get; } =
        All.Select(m => m.Key).ToList();

    public static IReadOnlyList<string> DefaultKeys { get; } =
        [Dashboard, Enquiries, ClientLeads, ActiveDeals, FollowUps, Employees, Punch];

    public static IEnumerable<IGrouping<string, AdminMenuItem>> BySection() =>
        All.GroupBy(m => m.Section);

    public static string? KeyForAction(string? action) => action switch
    {
        "Index" => Dashboard,
        "Enquiries" or "EnquiryDetails" or "EditEnquiry" or "AddEnquiryNote" or "UpdateEnquiryStatus" or "DeleteEnquiry" or "BulkDeleteEnquiries" => Enquiries,
        "DemoRequests" or "DemoRequestDetails" or "EditDemoRequest" or "AddDemoRequestNote" or "UpdateDemoRequestStatus" or "DeleteDemoRequest" or "BulkDeleteDemoRequests" => DemoRequests,
        "ClientLeads" or "AddClientLead" or "ClientLeadDetails" or "EditClientLead" or "AddClientLeadNote" or "UpdateClientLeadStatus" or "CheckDuplicateLead" => ClientLeads,
        "MergeLeads" => MergeLeads,
        "Services" or "AddService" or "EditService" or "ServiceDetails" or "AddServicePanel" or "DeleteServicePanel" or "UploadPanelModules" or "DownloadServiceModulesTemplate" or "GetServiceModules" or "ToggleService" or "DeleteService" => Services,
        "ChannelPartners" or "AddChannelPartner" or "EditChannelPartner" or "ChannelPartnerDetails" or "ToggleChannelPartner" or "DeleteChannelPartner" or "SendPartnerCredentials" => ChannelPartners,
        "PartnerClients" or "PartnerClientDetails" => PartnerClients,
        "PartnerLeadTracking" or "AssignLeadToPartner" or "UnassignLeadFromPartner" => PartnerLeadTracking,
        "PartnerMeetings" or "AddPartnerMeeting" or "TogglePartnerMeeting" or "DeletePartnerMeeting" => PartnerMeetings,
        "PartnerTickets" or "ReplyPartnerTicket" => PartnerTickets,
        "MarketingKitAdmin" or "AddMarketingKitItem" or "ViewMarketingKitItem" or "ToggleMarketingKitItem" or "DeleteMarketingKitItem" => MarketingKit,

        "PipelineBoard" => PipelineBoard,
        "ConfirmedClients" => ActiveDeals,
        "LeadTasks" or "AddLeadTask" or "CompleteLeadTask" => LeadTasks,
        "FollowUps" or "AddFollowUp" or "CompleteFollowUp" => FollowUps,
        "FollowUpAutomation" or "NudgeFollowUp" => FollowUpNudges,
        "RejectedClients" => Rejected,

        "Proposals" or "DownloadProposal" or "DownloadPartnerProposal" or "GenerateProposal" or "SendProposalEmail" or "CreateProposalRevision" => Proposals,
        "Invoices" or "ConvertToInvoice" or "RecordInvoicePayment" or "MarkInvoicePaid" or "DownloadInvoice" => Invoices,
        "InvoiceReminders" or "SendInvoiceReminder" => InvoiceReminders,
        "RecurringInvoices" or "ToggleRecurringInvoice" or "GenerateDueRecurring" => RecurringInvoices,

        "Employees" or "AddEmployee" or "EditEmployee" or "EmployeeDetails" or "DeleteEmployee" or "ToggleEmployee"
            or "EmployeeFiles" or "DownloadEmployeeFile" or "DeleteEmployeeFile" or "SetEmployeeManager" or "SalaryStructure" => Employees,
        "PunchAttendance" => Punch,
        "AttendanceReport" or "AttendanceReportExport" => AttendanceReport,
        "LeaveRequests" or "ApproveLeave" or "RejectLeave" => Leave,
        "Holidays" or "AddHoliday" or "DeleteHoliday" or "ToggleHoliday" => Holidays,
        "EmployeeDocumentTemplates" or "AddEmployeeDocumentTemplate" or "EditEmployeeDocumentTemplate" or "DeleteEmployeeDocumentTemplate"
            or "GenerateEmployeeDocument" or "PreviewEmployeeDocument" or "DownloadEmployeeDocument" or "WhatsAppEmployeeDocument"
            or "EmailEmployeeDocument" or "DeleteEmployeeDocument" => DocTemplates,
        "BulkGenerateDocuments" => BulkDocuments,
        "Payslips" or "GeneratePayslip" => Payslips,

        "SalesSummary" => SalesSummary,
        "PartnerPerformance" => PartnerPerformance,
        "CommissionTracker" or "MarkCommissionPaid" or "MarkCommissionUnpaid" => Commission,
        "Activity" => Activity,

        "Settings" or "UpdateCompany" or "UpdateContacts" or "UpdateSignature" or "UpdateSecurity" or "UpdateSmtp" or "UpdateEmployeeAccess" or "SendEmail" => Settings,
        "MessageTemplates" or "AddMessageTemplate" or "EditMessageTemplate" or "DeleteMessageTemplate" or "GetMessageTemplates" => MessageTemplates,
        "AdminUsers" or "AddAdminUser" or "ToggleAdminUser" or "ResetAdminPassword" or "UpdateAdminAccess" => AdminUsers,
        "Notifications" or "MarkNotificationRead" or "MarkAllNotificationsRead" => Notifications,
        "AuditLogs" => AuditLogs,
        "EmailLogs" => EmailLogs,
        "DataExport" or "ExportEmployeesCsv" or "ExportInvoicesCsv" or "ExportLeadsCsv" => DataExport,
        "WhatsAppSettings" or "SaveWhatsAppSettings" => WhatsApp,

        "Search" or "SearchSuggest" or "DashboardChartsJson" => Dashboard,
        _ => null
    };
}
