using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnWeb.Models;

public class QuestionTag
{
    [Key]
    public int QuestionTagId { get; set; }

    [Required]
    [ForeignKey("Question")]
    public int QuestionId { get; set; }

    [Required]
    [ForeignKey("Tag")]
    public int TagId { get; set; }

    public virtual Question Question { get; set; } = null!;
    public virtual Tag Tag { get; set; } = null!;
}