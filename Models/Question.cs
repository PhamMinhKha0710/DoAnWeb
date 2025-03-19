using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

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

    public bool IsSaved { get; set; } 

    [NotMapped]
    public string? UserVoteType { get; set; }

    public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();

    public virtual User? User { get; set; }

    public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();

    public virtual ICollection<QuestionTag> QuestionTags { get; set; } = new List<QuestionTag>();
    
    public virtual ICollection<QuestionAttachment> Attachments { get; set; } = new List<QuestionAttachment>();
}
