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

    public virtual User? User { get; set; }
}
