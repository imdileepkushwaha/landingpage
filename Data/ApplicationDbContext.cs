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
    public DbSet<AdminUser> AdminUsers { get; set; }
    public DbSet<AdminSetting> AdminSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
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
