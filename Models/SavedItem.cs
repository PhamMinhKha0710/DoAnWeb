using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnWeb.Models
{
    /// <summary>
    /// Mô hình dữ liệu đại diện cho một mục được lưu bởi người dùng
    /// </summary>
    public class SavedItem
    {
        [Key]
        public int SavedItemId { get; set; }

        public int UserId { get; set; }
        
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        /// <summary>
        /// Loại mục được lưu (Question, Answer, etc.)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string ItemType { get; set; } = string.Empty;

        /// <summary>
        /// ID của mục được lưu
        /// </summary>
        [Required]
        public int ItemId { get; set; }

        /// <summary>
        /// Ngày lưu mục
        /// </summary>
        public DateTime SavedDate { get; set; } = DateTime.Now;
        
        /// <summary>
        /// Ghi chú tùy chọn của người dùng
        /// </summary>
        [MaxLength(500)]
        public string? Note { get; set; }
    }
} 