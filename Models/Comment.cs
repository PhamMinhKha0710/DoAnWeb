using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnWeb.Models;

public class Comment
{
    [Key]
    public int CommentId { get; set; }
    
    public string Body { get; set; } = string.Empty;
    
    public string TargetType { get; set; } = string.Empty;
    
    public int TargetId { get; set; }
    
    [Required]
    public DateTime CreatedDate { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [ForeignKey("UserId")]
    public User User { get; set; }
    
    public int? QuestionId { get; set; }
    
    [ForeignKey("QuestionId")]
    public Question Question { get; set; }
    
    public int? AnswerId { get; set; }
    
    [ForeignKey("AnswerId")]
    public Answer Answer { get; set; }
}
