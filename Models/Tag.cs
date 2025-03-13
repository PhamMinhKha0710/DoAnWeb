using System;
using System.Collections.Generic;

namespace DoAnWeb.Models;

public partial class Tag
{
    public int TagId { get; set; }

    public string TagName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

    public virtual ICollection<QuestionTag> QuestionTags { get; set; } = new List<QuestionTag>();
}
