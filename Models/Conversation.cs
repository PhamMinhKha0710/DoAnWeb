using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DoAnWeb.Models
{
    /// <summary>
    /// Represents a conversation between users
    /// </summary>
    public class Conversation
    {
        [Key]
        public int ConversationId { get; set; }
        
        public string Title { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime LastActivityAt { get; set; }
        
        // Navigation properties
        public virtual ICollection<Message> Messages { get; set; }
        
        public virtual ICollection<ConversationParticipant> Participants { get; set; }
        
        public Conversation()
        {
            Messages = new HashSet<Message>();
            Participants = new HashSet<ConversationParticipant>();
            CreatedAt = DateTime.UtcNow;
            LastActivityAt = DateTime.UtcNow;
        }
    }
} 