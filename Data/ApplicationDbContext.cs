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

        // Seed default admin user (password: admin123)
        // In a real app, use proper password hashing
        modelBuilder.Entity<AdminUser>().HasData(new AdminUser
        {
            Id = 1,
            Username = "admin",
            PasswordHash = "admin123"
        });
    }
}
