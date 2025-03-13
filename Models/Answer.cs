using System;
using System.Collections.Generic;

namespace DoAnWeb.Models;

public partial class Answer
{
    public int AnswerId { get; set; }

    public int? QuestionId { get; set; }

    public int? UserId { get; set; }

    public string Body { get; set; } = null!;

    public DateTime? CreatedDate { get; set; }

    public int? Score { get; set; }

    public bool IsAccepted { get; set; }

    public DateTime UpdatedDate { get; set; }

    public int? IsUpvote { get; set; }

    public virtual Question? Question { get; set; }

    public virtual User? User { get; set; }
}
