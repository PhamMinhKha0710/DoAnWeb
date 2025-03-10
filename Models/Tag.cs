using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnWeb.Models;

public partial class Tag
{
    [Key]
    public int TagId { get; set; }

    [Required]
    [StringLength(50)]
    public string TagName { get; set; } = null!;

    [StringLength(200)]
    public string? Description { get; set; }

    public virtual ICollection<QuestionTag> QuestionTags { get; set; } = new List<QuestionTag>();
    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
}
