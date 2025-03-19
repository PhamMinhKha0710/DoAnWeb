using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnWeb.Models;

public partial class Comment
{
    public int CommentId { get; set; }

    public int? UserId { get; set; }

    public string TargetType { get; set; } = null!;

    public int TargetId { get; set; }

    public int? QuestionId { get; set; }

    public int? AnswerId { get; set; }

    public string Body { get; set; } = null!;

    public DateTime? CreatedDate { get; set; }

    public virtual User? User { get; set; }
    
    [ForeignKey("QuestionId")]
    public virtual Question? Question { get; set; }
    
    [ForeignKey("AnswerId")]
    public virtual Answer? Answer { get; set; }
}
