using Microsoft.EntityFrameworkCore;
using backend.Models;

namespace backend.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Challenge> Challenges { get; set; }
    public DbSet<ChallengeParticipant> ChallengeParticipants { get; set; }
    public DbSet<Activity> Activities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User entity configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.GarminUserId).IsUnique();
            
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Username).HasMaxLength(100);
            entity.Property(e => e.GarminUserId).HasMaxLength(100);
            entity.Property(e => e.GarminAccessToken).HasMaxLength(500);
            entity.Property(e => e.GarminRefreshToken).HasMaxLength(500);
        });

        // Challenge entity configuration
        modelBuilder.Entity<Challenge>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            
            entity.HasOne(e => e.CreatedBy)
                .WithMany(u => u.CreatedChallenges)
                .HasForeignKey(e => e.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ChallengeParticipant entity configuration
        modelBuilder.Entity<ChallengeParticipant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.HasIndex(e => new { e.ChallengeId, e.UserId }).IsUnique();
            
            entity.Property(e => e.CurrentTotal).HasPrecision(18, 2);
            
            entity.HasOne(e => e.Challenge)
                .WithMany(c => c.Participants)
                .HasForeignKey(e => e.ChallengeId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.User)
                .WithMany(u => u.ChallengeParticipations)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Activity entity configuration
        modelBuilder.Entity<Activity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.HasIndex(e => e.GarminActivityId).IsUnique();
            
            entity.Property(e => e.GarminActivityId).HasMaxLength(100);
            entity.Property(e => e.ActivityName).HasMaxLength(255);
            entity.Property(e => e.Distance).HasPrecision(18, 2);
            entity.Property(e => e.ElevationGain).HasPrecision(18, 2);
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.Activities)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}