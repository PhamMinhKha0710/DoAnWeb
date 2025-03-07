using System;
using System.Collections.Generic;

namespace DoAnWeb.Models;

public partial class Question
{
    public int QuestionId { get; set; }

    public int? UserId { get; set; }

    public string Title { get; set; } = null!;

    public string Body { get; set; } = null!;

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string? Status { get; set; }

    public int? ViewCount { get; set; }

    public int? Score { get; set; }

    public virtual User? User { get; set; }

    public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();
}
