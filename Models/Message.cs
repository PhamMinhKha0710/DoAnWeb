using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnWeb.Models
{
    /// <summary>
    /// Represents a message in a conversation
    /// </summary>
    public class Message
    {
        [Key]
        public int MessageId { get; set; }
        
        public int ConversationId { get; set; }
        
        public int SenderId { get; set; }
        
        public string Content { get; set; } = null!;
        
        public DateTime SentAt { get; set; }
        
        public bool IsRead { get; set; }
        
        public DateTime? ReadAt { get; set; }
        
        // Navigation properties
        [ForeignKey("ConversationId")]
        public virtual Conversation Conversation { get; set; } = null!;
        
        [ForeignKey("SenderId")]
        public virtual User Sender { get; set; } = null!;
    }
} 