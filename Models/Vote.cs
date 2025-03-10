using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnWeb.Models;

public partial class Vote
{
    [Key]
    public int VoteId { get; set; }

    [Required]
    [ForeignKey("User")]
    public int UserId { get; set; }

    [Required]
    [StringLength(20)]
    public string TargetType { get; set; } = null!;

    [Required]
    public int TargetId { get; set; }

    [Required]
    public int VoteValue { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public virtual User User { get; set; } = null!;
    
    [Required]
    public bool IsUpvote { get; set; }
}
