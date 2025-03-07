using System;
using System.Collections.Generic;

namespace DoAnWeb.Models;

public partial class Vote
{
    public int VoteId { get; set; }

    public int? UserId { get; set; }

    public string TargetType { get; set; } = null!;

    public int TargetId { get; set; }

    public int VoteValue { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual User? User { get; set; }
    
    // Added property to fix service errors
    public bool IsUpvote { get; set; }
}
