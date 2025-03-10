using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnWeb.Models;

public partial class Answer
{
    [Key]
    public int AnswerId { get; set; }

    [Required]
    [ForeignKey("Question")]
    public int QuestionId { get; set; }

    [Required]
    [ForeignKey("User")]
    public int UserId { get; set; }

    [Required]
    public string Body { get; set; } = null!;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

    public int Score { get; set; } = 0;

    public bool IsAccepted { get; set; } = false;

    public virtual Question Question { get; set; } = null!;

    public virtual User User { get; set; } = null!;

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual ICollection<Vote> Votes { get; set; } = new List<Vote>();
}
