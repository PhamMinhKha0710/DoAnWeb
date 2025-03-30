using System;
using System.Collections.Generic;

namespace DoAnWeb.Models;

public partial class Notification
{
    public int NotificationId { get; set; }

    public int? UserId { get; set; }

    public string Title { get; set; } = null!;

    public string? Message { get; set; }

    public string? Url { get; set; }

    public bool? IsRead { get; set; }

    public DateTime? CreatedDate { get; set; }

    // Add notification type for categorization
    public string NotificationType { get; set; } = "General";

    // Add reference to related entity (e.g., Question, Answer)
    public int? RelatedEntityId { get; set; }

    public virtual User? User { get; set; }
}

// Enum-like class for notification types
public static class NotificationTypeConstants
{
    public const string Answer = "Answer";
    public const string Comment = "Comment";
    public const string Vote = "Vote";
    public const string Accept = "Accept";
    public const string Mention = "Mention";
    public const string ReputationChange = "ReputationChange";
    public const string General = "General";
    public const string System = "System";
}
