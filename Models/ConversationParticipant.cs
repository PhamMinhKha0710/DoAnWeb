using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnWeb.Models
{
    /// <summary>
    /// Represents a participant in a conversation
    /// </summary>
    public class ConversationParticipant
    {
        [Key]
        public int ParticipantId { get; set; }
        
        public int ConversationId { get; set; }
        
        public int UserId { get; set; }
        
        public bool IsArchived { get; set; }
        
        public bool IsMuted { get; set; }
        
        // Navigation properties
        [ForeignKey("ConversationId")]
        public virtual Conversation Conversation { get; set; }
        
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
} 