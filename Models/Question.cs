using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnWeb.Models;

public partial class Question
{
    [Key]
    public int QuestionId { get; set; }

    [Required]
    [ForeignKey("User")]
    public int UserId { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = null!;

    [Required]
    public string Body { get; set; } = null!;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedDate { get; set; }

    [StringLength(50)]
    public string? Status { get; set; }

    public int ViewCount { get; set; } = 0;

    public int Score { get; set; } = 0;

    public virtual User User { get; set; } = null!;

    public virtual ICollection<QuestionTag> QuestionTags { get; set; } = new List<QuestionTag>();
    public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();

    public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();
}
