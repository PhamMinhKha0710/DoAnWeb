using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using DoAnWeb.Models;

namespace DoAnWeb;

public partial class DevCommunityContext : DbContext
{
    public DevCommunityContext(DbContextOptions<DevCommunityContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ActivityLog> ActivityLogs { get; set; }

    public virtual DbSet<Answer> Answers { get; set; }

    public virtual DbSet<Comment> Comments { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Question> Questions { get; set; }

    public virtual DbSet<Repository> Repositories { get; set; }

    public virtual DbSet<RepositoryCommit> RepositoryCommits { get; set; }

    public virtual DbSet<RepositoryFile> RepositoryFiles { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Tag> Tags { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserWatchedTag> UserWatchedTags { get; set; }

    public virtual DbSet<UserIgnoredTag> UserIgnoredTags { get; set; }

    public virtual DbSet<Vote> Votes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__Activity__5E548648B9E8B75E");

            entity.Property(e => e.ActivityType).HasMaxLength(50);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Details).HasMaxLength(500);

            entity.HasOne(d => d.User).WithMany(p => p.ActivityLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__ActivityL__UserI__05D8E0BE");
        });

        modelBuilder.Entity<Answer>(entity =>
        {
            entity.HasKey(e => e.AnswerId).HasName("PK__Answers__D482500454278ED7");

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Score).HasDefaultValue(0);

            entity.HasOne(d => d.Question).WithMany(p => p.Answers)
                .HasForeignKey(d => d.QuestionId)
                .HasConstraintName("FK__Answers__Questio__2CF2ADDF");

            entity.HasOne(d => d.User).WithMany(p => p.Answers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Answers__UserId__2DE6D218");
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.CommentId).HasName("PK__Comments__C3B4DFCA2B6300D8");

            entity.Property(e => e.Body).HasMaxLength(500);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TargetType).HasMaxLength(20);

            entity.HasOne(d => d.User).WithMany(p => p.Comments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Comments__UserId__6FE99F9F");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E121630D030");

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
                .HasConstraintName("FK__Notificat__UserI__01142BA1");
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.QuestionId).HasName("PK__Question__0DC06FACBE11A16C");

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
                .UsingEntity<Dictionary<string, object>>(
                    "QuestionTag",
                    r => r.HasOne<Tag>().WithMany()
                        .HasForeignKey("TagId")
                        .HasConstraintName("FK__QuestionT__TagId__787EE5A0"),
                    l => l.HasOne<Question>().WithMany()
                        .HasForeignKey("QuestionId")
                        .HasConstraintName("FK__QuestionT__Quest__778AC167"),
                    j =>
                    {
                        j.HasKey("QuestionId", "TagId").HasName("PK__Question__DB97A0365C4E806E");
                        j.ToTable("QuestionTags");
                    });
        });

        modelBuilder.Entity<Repository>(entity =>
        {
            entity.HasKey(e => e.RepositoryId).HasName("PK__Reposito__B9BA861124E5378E");

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
            entity.HasKey(e => e.CommitId).HasName("PK__Reposito__73748B725CA9C16C");

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
            entity.HasKey(e => e.FileId).HasName("PK__Reposito__6F0F98BF9CE85DF4");

            entity.Property(e => e.FileHash).HasMaxLength(64);
            entity.Property(e => e.FilePath).HasMaxLength(255);

            entity.HasOne(d => d.Repository).WithMany(p => p.RepositoryFiles)
                .HasForeignKey(d => d.RepositoryId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Repositor__Repos__60A75C0F");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE1A4DB30FEF");

            entity.HasIndex(e => e.RoleName, "UQ__Roles__8A2B61606CB05E4A").IsUnique();

            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.TagId).HasName("PK__Tags__657CF9AC2A664FD4");

            entity.HasIndex(e => e.TagName, "UQ__Tags__BDE0FD1D4BAE65EF").IsUnique();

            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.TagName).HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C59199BA3");

            entity.HasIndex(e => e.Username, "UQ__Users__536C85E49175E97B").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Users__A9D105341F829C62").IsUnique();

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
                        j.HasKey("UserId", "RoleId").HasName("PK__UserRole__AF2760AD26B2AEA4");
                        j.ToTable("UserRoles");
                    });
        });

        modelBuilder.Entity<Vote>(entity =>
        {
            entity.HasKey(e => e.VoteId).HasName("PK__Votes__52F015C2C4A741A0");

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TargetType).HasMaxLength(20);

            entity.HasOne(d => d.User).WithMany(p => p.Votes)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Votes__UserId__7B5B524B");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
