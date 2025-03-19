using System;

namespace DoAnWeb.Models;

public partial class ConversationParticipant
{
    public int ParticipantId { get; set; }

    public int ConversationId { get; set; }

    public int UserId { get; set; }

    public DateTime JoinedAt { get; set; }

    public bool IsAdmin { get; set; }

    public bool IsArchived { get; set; }

    public bool IsMuted { get; set; }

    public virtual Conversation Conversation { get; set; } = null!;

    public virtual User User { get; set; } = null!;
} 