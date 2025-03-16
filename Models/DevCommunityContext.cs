using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace DoAnWeb.Models;

public partial class DevCommunityContext : DbContext
{
    public DevCommunityContext(DbContextOptions<DevCommunityContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ActivityLog> ActivityLogs { get; set; }

    public virtual DbSet<Answer> Answers { get; set; }
    
    public virtual DbSet<AnswerAttachment> AnswerAttachments { get; set; }

    public virtual DbSet<Comment> Comments { get; set; }

    public virtual DbSet<Conversation> Conversations { get; set; }

    public virtual DbSet<ConversationParticipant> ConversationParticipants { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Question> Questions { get; set; }

    public virtual DbSet<QuestionAttachment> QuestionAttachments { get; set; }

    public virtual DbSet<Repository> Repositories { get; set; }

    public virtual DbSet<RepositoryCommit> RepositoryCommits { get; set; }

    public virtual DbSet<RepositoryFile> RepositoryFiles { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Tag> Tags { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserIgnoredTag> UserIgnoredTags { get; set; }

    public virtual DbSet<UserSavedItem> UserSavedItems { get; set; }

    public virtual DbSet<UserWatchedTag> UserWatchedTags { get; set; }

    public virtual DbSet<Vote> Votes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            #warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
            // Connection string is now configured through Dependency Injection in Program.cs
            // This is only used as a fallback if DI doesn't configure the context
            optionsBuilder.UseSqlServer("Server=DESKTOP-TQFDM4P\\SQLEXPRESS;Database=DevCommunity;Trusted_Connection=True;TrustServerCertificate=True;");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__Activity__5E54864859F09C42");

            entity.Property(e => e.ActivityType).HasMaxLength(50);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Details).HasMaxLength(500);

            entity.HasOne(d => d.User).WithMany(p => p.ActivityLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__ActivityL__UserI__0B91BA14");
        });

        modelBuilder.Entity<Answer>(entity =>
        {
            entity.HasKey(e => e.AnswerId).HasName("PK__Answers__D48250043037DE3B");

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Score).HasDefaultValue(0);
            entity.Property(e => e.UpdatedDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Question).WithMany(p => p.Answers)
                .HasForeignKey(d => d.QuestionId)
                .HasConstraintName("FK__Answers__Questio__6FE99F9F");

            entity.HasOne(d => d.User).WithMany(p => p.Answers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Answers__UserId__70DDC3D8");
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.CommentId).HasName("PK__Comments__C3B4DFCA7DE7D79F");

            entity.Property(e => e.Body).HasMaxLength(500);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TargetType).HasMaxLength(20);

            entity.HasOne(d => d.User).WithMany(p => p.Comments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Comments__UserId__75A278F5");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E1291F3BFF1");

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.Message).HasMaxLength(500);
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.Url).HasMaxLength(255);

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Notificat__UserI__06CD04F7");
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.QuestionId).HasName("PK__Question__0DC06FACC2370EE1");

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Score).HasDefaultValue(0);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Open");
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.UpdatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ViewCount).HasDefaultValue(0);

            entity.HasOne(d => d.User).WithMany(p => p.Questions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Questions__UserI__6383C8BA");

            entity.HasMany(d => d.Tags).WithMany(p => p.Questions)
                .UsingEntity<QuestionTag>(
                    r => r.HasOne(qt => qt.Tag).WithMany(t => t.QuestionTags)
                        .HasForeignKey(qt => qt.TagId)
                        .HasConstraintName("FK__QuestionT__TagId__7E37BEF6"),
                    l => l.HasOne(qt => qt.Question).WithMany(q => q.QuestionTags)
                        .HasForeignKey(qt => qt.QuestionId)
                        .HasConstraintName("FK__QuestionT__Quest__7D439ABD"),
                    j =>
                    {
                        j.HasKey(qt => new { qt.QuestionId, qt.TagId }).HasName("PK__Question__DB97A036892C4093");
                        j.ToTable("QuestionTags");
                    });
        
        });

        modelBuilder.Entity<Repository>(entity =>
        {
            entity.HasKey(e => e.RepositoryId).HasName("PK__Reposito__B9BA861130E27F9B");

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DefaultBranch).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.RepositoryName).HasMaxLength(100);
            entity.Property(e => e.UpdatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Visibility).HasMaxLength(20);

            entity.HasOne(d => d.Owner).WithMany(p => p.Repositories)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Repositor__Owner__5629CD9C");
        });

        modelBuilder.Entity<RepositoryCommit>(entity =>
        {
            entity.HasKey(e => e.CommitId).HasName("PK__Reposito__73748B72889F7284");

            entity.Property(e => e.CommitDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.CommitMessage).HasMaxLength(255);

            entity.HasOne(d => d.Author).WithMany(p => p.RepositoryCommits)
                .HasForeignKey(d => d.AuthorId)
                .HasConstraintName("FK__Repositor__Autho__5CD6CB2B");

            entity.HasOne(d => d.Repository).WithMany(p => p.RepositoryCommits)
                .HasForeignKey(d => d.RepositoryId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Repositor__Repos__5BE2A6F2");
        });

        modelBuilder.Entity<RepositoryFile>(entity =>
        {
            entity.HasKey(e => e.FileId).HasName("PK__Reposito__6F0F98BF7E131276");

            entity.Property(e => e.FileHash).HasMaxLength(64);
            entity.Property(e => e.FilePath).HasMaxLength(255);

            entity.HasOne(d => d.Repository).WithMany(p => p.RepositoryFiles)
                .HasForeignKey(d => d.RepositoryId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Repositor__Repos__60A75C0F");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE1A0EBEA49E");

            entity.HasIndex(e => e.RoleName, "UQ__Roles__8A2B61601DB624C4").IsUnique();

            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.TagId).HasName("PK__Tags__657CF9ACCCDEB8E9");

            entity.HasIndex(e => e.TagName, "UQ__Tags__BDE0FD1DE35F6B5A").IsUnique();

            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.TagName).HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4CE4378A70");

            entity.HasIndex(e => e.Username, "UQ__Users__536C85E44593A637").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Users__A9D105348AA87732").IsUnique();

            entity.Property(e => e.AvatarUrl).HasMaxLength(255);
            entity.Property(e => e.Bio).HasMaxLength(500);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DisplayName).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.UpdatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Username).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastLoginDate)
                .HasColumnType("datetime")
                .IsRequired(false);

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserRole",
                    r => r.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .HasConstraintName("FK__UserRoles__RoleI__534D60F1"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .HasConstraintName("FK__UserRoles__UserI__52593CB8"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId").HasName("PK__UserRole__AF2760AD4D77315A");
                        j.ToTable("UserRoles");
                    });
        });

        modelBuilder.Entity<Vote>(entity =>
        {
            entity.HasKey(e => e.VoteId).HasName("PK__Votes__52F015C2EC70E0B2");

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TargetType).HasMaxLength(20);

            entity.HasOne(d => d.User).WithMany(p => p.Votes)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Votes__UserId__01142BA1");
        
            entity.HasOne(d => d.Answer).WithMany()
                .HasForeignKey(d => d.AnswerId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Votes__AnswerId__01142BA2");
        });

        modelBuilder.Entity<UserSavedItem>(entity =>
        {
            entity.HasKey(e => e.SavedItemId).HasName("PK__UserSave__C7D2D2E3A1B2C3D4");

            entity.Property(e => e.ItemType).HasMaxLength(20);
            entity.Property(e => e.SavedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.User).WithMany(p => p.UserSavedItems)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__UserSaved__UserI__123456789");
        });

        // Chat Models Configuration
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.ConversationId);
            
            entity.Property(e => e.Title).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.LastActivityAt).HasDefaultValueSql("(getdate())");
        });
        
        modelBuilder.Entity<ConversationParticipant>(entity =>
        {
            entity.HasKey(e => e.ParticipantId);
            
            entity.HasOne(d => d.Conversation)
                .WithMany(p => p.Participants)
                .HasForeignKey(d => d.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.MessageId);
            
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.SentAt).HasDefaultValueSql("(getdate())");
            
            entity.HasOne(d => d.Conversation)
                .WithMany(p => p.Messages)
                .HasForeignKey(d => d.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(d => d.Sender)
                .WithMany()
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
