using System;
using System.Collections.Generic;

namespace DoAnWeb.Models;

public partial class UserSavedItem
{
    public int SavedItemId { get; set; }

    public int UserId { get; set; }

    public string ItemType { get; set; } = null!; // "Question" or "Answer"

    public int ItemId { get; set; } // QuestionId or AnswerId

    public DateTime SavedDate { get; set; }

    public virtual User User { get; set; } = null!;
}