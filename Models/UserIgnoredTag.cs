using System;
using System.Collections.Generic;

namespace DoAnWeb.Models;

public partial class UserIgnoredTag
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int TagId { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual Tag Tag { get; set; } = null!;
}