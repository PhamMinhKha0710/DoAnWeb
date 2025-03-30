using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnWeb.Models
{
    /// <summary>
    /// Mô hình dữ liệu đại diện cho việc gán huy hiệu cho người dùng
    /// </summary>
    public class BadgeAssignment
    {
        [Key]
        public int BadgeAssignmentId { get; set; }
        
        /// <summary>
        /// ID của người dùng nhận huy hiệu
        /// </summary>
        public int UserId { get; set; }
        
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
        
        /// <summary>
        /// ID của huy hiệu
        /// </summary>
        public int BadgeId { get; set; }
        
        [ForeignKey("BadgeId")]
        public virtual Badge? Badge { get; set; }
        
        /// <summary>
        /// Ngày nhận huy hiệu
        /// </summary>
        public DateTime AwardedDate { get; set; } = DateTime.Now;
        
        /// <summary>
        /// Lý do nhận huy hiệu
        /// </summary>
        [MaxLength(500)]
        public string? Reason { get; set; }
        
        /// <summary>
        /// Ghi chú
        /// </summary>
        [MaxLength(500)]
        public string? Note { get; set; }
        
        /// <summary>
        /// Có thông báo cho người dùng khi được nhận huy hiệu hay không
        /// </summary>
        public bool Notified { get; set; } = false;
    }
} 