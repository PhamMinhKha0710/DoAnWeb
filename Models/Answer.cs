using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

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
    
    public bool IsSaved {get; set;}

    public DateTime UpdatedDate { get; set; }

    public int? IsUpvote { get; set; }
    
    [NotMapped]
    public string? UserVoteType { get; set; }
    
    // Property to track which answer this one is responding to
    public int? ParentAnswerId { get; set; }
    
    [ForeignKey("ParentAnswerId")]
    public virtual Answer? ParentAnswer { get; set; }
    
    [InverseProperty("ParentAnswer")]
    public virtual ICollection<Answer>? ChildAnswers { get; set; }

    public virtual Question? Question { get; set; }

    public virtual User? User { get; set; }
    
    public virtual ICollection<AnswerAttachment>? Attachments { get; set; }

    [NotMapped]
    public virtual ICollection<Comment>? Comments { get; set; }
}
