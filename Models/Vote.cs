using System;
using System.Collections.Generic;

namespace DoAnWeb.Models;

public partial class Vote
{
    public int VoteId { get; set; }

    public int? UserId { get; set; }

    public string TargetType { get; set; } = null!;

    public int TargetId { get; set; }

    public int? AnswerId { get; set; }

    public int VoteValue { get; set; }

    public bool IsUpvote { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual User? User { get; set; }

    public virtual Answer? Answer { get; set; }
}
