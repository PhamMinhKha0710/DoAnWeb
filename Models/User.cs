using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnWeb.Models;

public partial class User
{
    [Key]
    public int UserId { get; set; }

    [Required]
    [StringLength(50)]
    public string Username { get; set; } = null!;

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = null!;

    [Required]
    public string PasswordHash { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string DisplayName { get; set; } = null!;

    [StringLength(255)]
    public string? AvatarUrl { get; set; }

    [StringLength(500)]
    public string? Bio { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

    public virtual ICollection<Repository> Repositories { get; set; } = new List<Repository>();

    public virtual ICollection<RepositoryCommit> RepositoryCommits { get; set; } = new List<RepositoryCommit>();

    public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();

    public virtual ICollection<Vote> Votes { get; set; } = new List<Vote>();

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
    
    public virtual ICollection<UserWatchedTag> WatchedTags { get; set; } = new List<UserWatchedTag>();
    
    public virtual ICollection<UserIgnoredTag> IgnoredTags { get; set; } = new List<UserIgnoredTag>();
}
