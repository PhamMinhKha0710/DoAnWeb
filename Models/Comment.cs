using System;
using System.Collections.Generic;

namespace DoAnWeb.Models;

public partial class Comment
{
    public int CommentId { get; set; }

    public int? UserId { get; set; }

    public string TargetType { get; set; } = null!;

    public int TargetId { get; set; }

    public string Body { get; set; } = null!;

    public DateTime? CreatedDate { get; set; }

    public virtual User? User { get; set; }
}
