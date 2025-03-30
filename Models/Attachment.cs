using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnWeb.Models
{
    /// <summary>
    /// Mô hình dữ liệu đại diện cho tệp đính kèm tổng quát trong hệ thống
    /// </summary>
    public class Attachment
    {
        [Key]
        public int AttachmentId { get; set; }
        
        /// <summary>
        /// ID của đối tượng liên quan (Question, Answer, Comment, etc.)
        /// </summary>
        public int? RelatedEntityId { get; set; }
        
        /// <summary>
        /// Loại đối tượng liên quan (Question, Answer, Comment, etc.)
        /// </summary>
        [MaxLength(50)]
        public string? RelatedEntityType { get; set; }
        
        /// <summary>
        /// Tên file gốc
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;
        
        /// <summary>
        /// Loại nội dung của file (MIME type)
        /// </summary>
        [MaxLength(100)]
        public string? ContentType { get; set; }
        
        /// <summary>
        /// Đường dẫn tới file trong hệ thống
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;
        
        /// <summary>
        /// Kích thước file tính bằng byte
        /// </summary>
        public long FileSize { get; set; }
        
        /// <summary>
        /// Ngày tải file lên
        /// </summary>
        public DateTime UploadDate { get; set; } = DateTime.Now;
        
        /// <summary>
        /// ID người dùng đã tải lên
        /// </summary>
        public int? UploadedByUserId { get; set; }
        
        [ForeignKey("UploadedByUserId")]
        public virtual User? UploadedByUser { get; set; }
        
        /// <summary>
        /// Mô tả file
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }
        
        /// <summary>
        /// Có hiển thị công khai tệp đính kèm hay không
        /// </summary>
        public bool IsPublic { get; set; } = true;
    }
} 