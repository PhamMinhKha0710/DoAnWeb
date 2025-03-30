using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnWeb.Models
{
    /// <summary>
    /// Lưu lịch sử thay đổi reputation của người dùng
    /// </summary>
    public class ReputationHistory
    {
        [Key]
        public int HistoryId { get; set; }
        
        public int UserId { get; set; }
        
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
        
        /// <summary>
        /// Lượng thay đổi (có thể dương hoặc âm)
        /// </summary>
        public int Amount { get; set; }
        
        /// <summary>
        /// Giá trị reputation trước khi thay đổi
        /// </summary>
        public int OldValue { get; set; }
        
        /// <summary>
        /// Giá trị reputation sau khi thay đổi
        /// </summary>
        public int NewValue { get; set; }
        
        /// <summary>
        /// Lý do thay đổi reputation
        /// </summary>
        public string Reason { get; set; }
        
        /// <summary>
        /// ID của entity liên quan (nếu có), có thể là QuestionId, AnswerId...
        /// </summary>
        public int? RelatedEntityId { get; set; }
        
        /// <summary>
        /// Loại entity liên quan, ví dụ "Question", "Answer"...
        /// </summary>
        public string RelatedEntityType { get; set; }
        
        /// <summary>
        /// Thời điểm thay đổi
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
} 