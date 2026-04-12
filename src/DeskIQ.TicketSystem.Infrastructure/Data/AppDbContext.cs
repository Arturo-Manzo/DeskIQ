using Microsoft.EntityFrameworkCore;
using DeskIQ.TicketSystem.Core.Entities;

namespace DeskIQ.TicketSystem.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<User> Users { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<TicketSequence> TicketSequences { get; set; }
    public DbSet<TicketMessage> TicketMessages { get; set; }
    public DbSet<TicketAttachment> TicketAttachments { get; set; }
    public DbSet<TicketActivity> TicketActivities { get; set; }
    public DbSet<EmailAccount> EmailAccounts { get; set; }
    public DbSet<WhatsAppConfig> WhatsAppConfigs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
            entity.HasOne(e => e.Department)
                  .WithMany(d => d.Users)
                  .HasForeignKey(e => e.DepartmentId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Department configuration
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(4);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.AutoAssignRules).HasMaxLength(2000);
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // Ticket configuration
        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TicketId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.ExternalId).HasMaxLength(100);
            entity.Property(e => e.BlockedReason).HasMaxLength(1000);
            entity.Property(e => e.IsBlocked).HasDefaultValue(false);
            entity.HasOne(e => e.CreatedBy)
                  .WithMany(u => u.CreatedTickets)
                  .HasForeignKey(e => e.CreatedById)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.AssignedTo)
                  .WithMany(u => u.AssignedTickets)
                  .HasForeignKey(e => e.AssignedToId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Department)
                  .WithMany(d => d.Tickets)
                  .HasForeignKey(e => e.DepartmentId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ParentTicket)
                .WithMany(t => t.Subtickets)
                .HasForeignKey(e => e.ParentTicketId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.TicketId).IsUnique();
            entity.HasIndex(e => e.ParentTicketId);
            entity.HasIndex(e => e.IsBlocked);
          });

          modelBuilder.Entity<TicketSequence>(entity =>
          {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Year).IsRequired();
            entity.Property(e => e.LastValue).IsRequired();
            entity.HasOne(e => e.Department)
                .WithMany(d => d.TicketSequences)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.DepartmentId, e.Year }).IsUnique();
        });

        // TicketMessage configuration
        modelBuilder.Entity<TicketMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired();
            entity.HasOne(e => e.Ticket)
                  .WithMany(t => t.Messages)
                  .HasForeignKey(e => e.TicketId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Sender)
                  .WithMany(u => u.Messages)
                  .HasForeignKey(e => e.SenderId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ParentMessage)
                .WithMany(m => m.Replies)
                .HasForeignKey(e => e.ParentMessageId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.ParentMessageId);
        });

        // TicketAttachment configuration
        modelBuilder.Entity<TicketAttachment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
            entity.HasOne(e => e.Ticket)
                  .WithMany(t => t.Attachments)
                  .HasForeignKey(e => e.TicketId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.UploadedBy)
                  .WithMany()
                  .HasForeignKey(e => e.UploadedById)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // TicketActivity configuration
        modelBuilder.Entity<TicketActivity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.OldValue).HasMaxLength(200);
            entity.Property(e => e.NewValue).HasMaxLength(200);
            entity.HasOne(e => e.Ticket)
                  .WithMany()
                  .HasForeignKey(e => e.TicketId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.PerformedBy)
                  .WithMany()
                  .HasForeignKey(e => e.PerformedById)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.TicketId);
            entity.HasIndex(e => e.CreatedAt);
        });

        // EmailAccount configuration
        modelBuilder.Entity<EmailAccount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Password).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ImapServer).IsRequired().HasMaxLength(255);
            entity.Property(e => e.SmtpServer).IsRequired().HasMaxLength(255);
            entity.HasOne(e => e.Department)
                  .WithMany(d => d.EmailAccounts)
                  .HasForeignKey(e => e.DepartmentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // WhatsAppConfig configuration
        modelBuilder.Entity<WhatsAppConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.AccessToken).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.WebhookSecret).IsRequired().HasMaxLength(255);
            entity.HasOne(e => e.Department)
                  .WithMany(d => d.WhatsAppConfigs)
                  .HasForeignKey(e => e.DepartmentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Create default admin department
        var adminDeptId = Guid.NewGuid();
        modelBuilder.Entity<Department>().HasData(
            new Department
            {
                Id = adminDeptId,
                Name = "IT Support",
                Code = "TI",
                Description = "Technical support department",
                AutoAssignRules = "{\"roundRobin\": true}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );

        // Create default admin user
        var adminUserId = Guid.NewGuid();
        var adminPasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"); // Temporary password
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = adminUserId,
                Name = "System Administrator",
                Email = "admin@deskiq.com",
                PasswordHash = adminPasswordHash,
                DepartmentId = adminDeptId,
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );
    }
}
