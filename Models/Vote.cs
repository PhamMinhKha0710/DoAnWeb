﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnWeb.Models;

public partial class Vote
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int VoteId { get; set; }

    public int? UserId { get; set; }

    public string TargetType { get; set; } = null!;

    public int TargetId { get; set; }

    public int? AnswerId { get; set; }

    public int VoteValue { get; set; }

    public bool IsUpvote { get; set; }

    public DateTime? CreatedDate { get; set; }
    
    [NotMapped]
    public DateTime VoteDate 
    {
        get { return CreatedDate ?? DateTime.Now; }
        set { CreatedDate = value; }
    }

    public virtual User? User { get; set; }

    public virtual Answer? Answer { get; set; }
}
