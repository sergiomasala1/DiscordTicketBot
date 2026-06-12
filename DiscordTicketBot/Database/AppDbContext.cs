using Microsoft.EntityFrameworkCore;
using DiscordTicketBot.Models;

namespace DiscordTicketBot.Database;

/// <summary>
/// Contexto do banco de dados SQLite via Entity Framework Core.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TimeSession> TimeSessions => Set<TimeSession>();
    public DbSet<PauseSession> PauseSessions => Set<PauseSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Users ──────────────────────────────────────────────────
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.DiscordUserId).IsUnique();
            entity.Property(u => u.DiscordUserId).IsRequired().HasMaxLength(20);
            entity.Property(u => u.Username).IsRequired().HasMaxLength(100);
            entity.Property(u => u.DisplayName).HasMaxLength(100);
        });

        // ── Tickets ────────────────────────────────────────────────
        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.HasIndex(t => new { t.UserId, t.Status });
            entity.Property(t => t.TicketNumber).IsRequired().HasMaxLength(50);
            entity.Property(t => t.Description).HasMaxLength(500);
            entity.Property(t => t.Status).HasConversion<int>();
            entity.Property(t => t.DiscordMessageId).HasMaxLength(20);
            entity.Property(t => t.DiscordChannelId).HasMaxLength(20);

            entity.HasOne(t => t.User)
                  .WithMany(u => u.Tickets)
                  .HasForeignKey(t => t.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── TimeSessions ───────────────────────────────────────────
        modelBuilder.Entity<TimeSession>(entity =>
        {
            entity.HasKey(ts => ts.Id);
            entity.HasIndex(ts => ts.TicketId);

            entity.HasOne(ts => ts.Ticket)
                  .WithMany(t => t.TimeSessions)
                  .HasForeignKey(ts => ts.TicketId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── PauseSessions ──────────────────────────────────────────
        modelBuilder.Entity<PauseSession>(entity =>
        {
            entity.HasKey(ps => ps.Id);
            entity.HasIndex(ps => ps.TicketId);

            entity.HasOne(ps => ps.Ticket)
                  .WithMany(t => t.PauseSessions)
                  .HasForeignKey(ps => ps.TicketId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
