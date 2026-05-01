using Microsoft.EntityFrameworkCore;
using TrueAltitude.Domain.Entities;

namespace TrueAltitude.Persistence.Data;

public class TrueAltitudeDbContext : DbContext
{
    public TrueAltitudeDbContext(DbContextOptions<TrueAltitudeDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Setting> Settings { get; set; } = null!;
    public DbSet<SubscriptionPurchase> SubscriptionPurchases { get; set; } = null!;
    public DbSet<LearningSubject> LearningSubjects { get; set; } = null!;
    public DbSet<LearningTopic> LearningTopics { get; set; } = null!;
    public DbSet<LearningTopicQuestion> LearningTopicQuestions { get; set; } = null!;
    public DbSet<LearningQuestion> LearningQuestions { get; set; } = null!;
    public DbSet<LearningQuestionOption> LearningQuestionOptions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User entity configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(500);

            entity.Property(e => e.Provider)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("local");

            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime(6)")
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

            entity.Property(e => e.OtpCode)
                .HasMaxLength(10);

            entity.Property(e => e.OtpExpiresAt)
                .HasColumnType("datetime(6)");

            entity.Property(e => e.SubscriptionStatus)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("none");

            entity.Property(e => e.SubscriptionPlanCode)
                .HasMaxLength(50);

            entity.Property(e => e.SubscriptionPlanName)
                .HasMaxLength(100);

            entity.Property(e => e.SubscriptionStartedAt)
                .HasColumnType("datetime(6)");

            entity.Property(e => e.SubscriptionExpiresAt)
                .HasColumnType("datetime(6)");

            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<Setting>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Key)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(e => e.ValueJson)
                .IsRequired()
                .HasColumnType("longtext");

            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime(6)");

            entity.HasIndex(e => e.Key).IsUnique();
        });

        modelBuilder.Entity<SubscriptionPurchase>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.PlanCode)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.PlanName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Currency)
                .IsRequired()
                .HasMaxLength(10)
                .HasDefaultValue("INR");

            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("pending");

            entity.Property(e => e.PaymentProvider)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("razorpay");

            entity.Property(e => e.ProviderOrderId)
                .HasMaxLength(120);

            entity.Property(e => e.ProviderPaymentId)
                .HasMaxLength(120);

            entity.Property(e => e.ProviderSignature)
                .HasMaxLength(255);

            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime(6)");

            entity.Property(e => e.PaidAt)
                .HasColumnType("datetime(6)");

            entity.Property(e => e.SubscriptionEndsAt)
                .HasColumnType("datetime(6)");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ProviderOrderId);
        });

        modelBuilder.Entity<LearningSubject>(entity =>
        {
            entity.ToTable("LearningSubjects");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Code)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.Property(e => e.SubscriptionLabel)
                .HasMaxLength(100);

            entity.Property(e => e.SortOrder)
                .HasDefaultValue(0);

            entity.HasIndex(e => e.Code)
                .IsUnique();
        });

        modelBuilder.Entity<LearningTopic>(entity =>
        {
            entity.ToTable("LearningTopics");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Code)
                .IsRequired()
                .HasMaxLength(120);

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(250);

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.Property(e => e.SubscriptionLabel)
                .HasMaxLength(100);

            entity.Property(e => e.SortOrder)
                .HasDefaultValue(0);

            entity.HasIndex(e => e.Code)
                .IsUnique();

            entity.HasOne(e => e.Subject)
                .WithMany(s => s.Topics)
                .HasForeignKey(e => e.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ParentTopic)
                .WithMany(t => t.Children)
                .HasForeignKey(e => e.ParentTopicId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.SubjectId, e.ParentTopicId, e.SortOrder });
        });

        modelBuilder.Entity<LearningQuestion>(entity =>
        {
            entity.ToTable("LearningQuestions");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Code)
                .IsRequired()
                .HasMaxLength(120);

            entity.Property(e => e.Text)
                .IsRequired()
                .HasColumnType("longtext");

            entity.Property(e => e.SubscriptionLabel)
                .HasMaxLength(100);

            entity.Property(e => e.SortOrder)
                .HasDefaultValue(0);

            entity.HasIndex(e => e.Code)
                .IsUnique();
        });

        modelBuilder.Entity<LearningTopicQuestion>(entity =>
        {
            entity.ToTable("LearningTopicQuestions");
            entity.HasKey(e => new { e.TopicId, e.QuestionId });

            entity.Property(e => e.SortOrder)
                .HasDefaultValue(0);

            entity.HasOne(e => e.Topic)
                .WithMany(t => t.TopicQuestions)
                .HasForeignKey(e => e.TopicId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Question)
                .WithMany(q => q.TopicQuestions)
                .HasForeignKey(e => e.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.TopicId, e.SortOrder });
            entity.HasIndex(e => e.QuestionId);
        });

        modelBuilder.Entity<LearningQuestionOption>(entity =>
        {
            entity.ToTable("LearningQuestionOptions");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Code)
                .IsRequired()
                .HasMaxLength(140);

            entity.Property(e => e.Text)
                .IsRequired()
                .HasColumnType("longtext");

            entity.Property(e => e.Explanation)
                .IsRequired()
                .HasColumnType("longtext");

            entity.Property(e => e.SortOrder)
                .HasDefaultValue(0);

            entity.HasIndex(e => e.Code)
                .IsUnique();

            entity.HasOne(e => e.Question)
                .WithMany(q => q.Options)
                .HasForeignKey(e => e.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.QuestionId, e.SortOrder });
        });
    }
}
