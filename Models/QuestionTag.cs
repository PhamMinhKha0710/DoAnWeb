using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DoAnWeb.Models;

[Table("QuestionTags")]
[PrimaryKey(nameof(QuestionId), nameof(TagId))]
public partial class QuestionTag
{
    public int QuestionId { get; set; }

    public int TagId { get; set; }

    public virtual Question Question { get; set; } = null!;

    public virtual Tag Tag { get; set; } = null!;
}