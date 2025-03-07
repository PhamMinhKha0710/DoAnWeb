using System;
using System.Collections.Generic;

namespace DoAnWeb.Models;

public partial class ActivityLog
{
    public int LogId { get; set; }

    public int? UserId { get; set; }

    public string ActivityType { get; set; } = null!;

    public string? Details { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual User? User { get; set; }
}
