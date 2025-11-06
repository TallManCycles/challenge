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
    public DbSet<ActivityLike> ActivityLikes { get; set; }
    public DbSet<GarminOAuthToken> GarminOAuthTokens { get; set; }
    public DbSet<GarminActivity> GarminActivities { get; set; }
    public DbSet<GarminWebhookPayload> GarminWebhookPayloads { get; set; }
    public DbSet<FitFileActivity> FitFileActivities { get; set; }
    public DbSet<Quote> Quotes { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

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

        // ActivityLike entity configuration
        modelBuilder.Entity<ActivityLike>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.HasIndex(e => new { e.ActivityId, e.UserId }).IsUnique();
            
            entity.HasOne(e => e.Activity)
                .WithMany()
                .HasForeignKey(e => e.ActivityId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // GarminOAuthToken entity configuration
        modelBuilder.Entity<GarminOAuthToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.State).IsUnique();
            entity.HasIndex(e => e.RequestToken);
            
            entity.Property(e => e.RequestToken).HasMaxLength(500);
            entity.Property(e => e.RequestTokenSecret).HasMaxLength(500);
            entity.Property(e => e.AccessToken).HasMaxLength(500);
            entity.Property(e => e.AccessTokenSecret).HasMaxLength(500);
            entity.Property(e => e.State).HasMaxLength(100);
            entity.Property(e => e.OAuthVerifier).HasMaxLength(100);
            
            // Set expiration default to 10 minutes from creation (PostgreSQL syntax)
            entity.Property(e => e.ExpiresAt)
                .HasDefaultValueSql("NOW() + INTERVAL '10 minutes'");
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // GarminActivity entity configuration
        modelBuilder.Entity<GarminActivity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.SummaryId).IsUnique();
            entity.HasIndex(e => e.ActivityType);
            entity.HasIndex(e => e.StartTime);
            entity.HasIndex(e => e.IsProcessed);
            entity.HasIndex(e => new { e.UserId, e.ActivityType });
            
            entity.Property(e => e.SummaryId).HasMaxLength(255);
            entity.Property(e => e.ActivityId).HasMaxLength(255);
            entity.Property(e => e.ActivityName).HasMaxLength(255);
            entity.Property(e => e.DeviceName).HasMaxLength(255);
            entity.Property(e => e.ActivityType)
                .HasConversion<string>();
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // GarminWebhookPayload entity configuration
        modelBuilder.Entity<GarminWebhookPayload>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.HasIndex(e => e.WebhookType);
            entity.HasIndex(e => e.IsProcessed);
            entity.HasIndex(e => e.ReceivedAt);
            entity.HasIndex(e => e.NextRetryAt);
            
            entity.Property(e => e.WebhookType)
                .HasConversion<string>();
        });

        // FitFileActivity entity configuration
        modelBuilder.Entity<FitFileActivity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.HasIndex(e => e.FileName).IsUnique();
            entity.HasIndex(e => e.ZwiftUserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ActivityDate);
            entity.HasIndex(e => e.CreatedAt);
            
            entity.Property(e => e.FileName).HasMaxLength(255);
            entity.Property(e => e.ZwiftUserId).HasMaxLength(100);
            entity.Property(e => e.ActivityName).HasMaxLength(255);
            entity.Property(e => e.ActivityType).HasMaxLength(50);
            entity.Property(e => e.ProcessingError).HasMaxLength(1000);
            entity.Property(e => e.Status)
                .HasConversion<string>();
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Quote entity configuration
        modelBuilder.Entity<Quote>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.HasIndex(e => e.Author);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsActive);

            entity.Property(e => e.Text).HasMaxLength(1000);
            entity.Property(e => e.Author).HasMaxLength(200);
            entity.Property(e => e.Category).HasMaxLength(100);
        });

        // RefreshToken entity configuration
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.UserId);
            // Composite index for efficient cleanup queries
            entity.HasIndex(e => new { e.UserId, e.RevokedAt, e.ExpiresAt });

            entity.Property(e => e.Token).HasMaxLength(500);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}