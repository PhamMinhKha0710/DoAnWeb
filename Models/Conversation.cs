using System;
using System.Collections.Generic;

namespace DoAnWeb.Models;

public partial class Conversation
{
    public int ConversationId { get; set; }

    public string Title { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime LastActivityAt { get; set; }

    public virtual ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
} 