using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DoAnWeb.Models
{
    /// <summary>
    /// Mô hình dữ liệu đại diện cho một huy hiệu trong hệ thống
    /// </summary>
    public class Badge
    {
        [Key]
        public int BadgeId { get; set; }
        
        /// <summary>
        /// Tên huy hiệu
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Mô tả về huy hiệu và cách đạt được
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }
        
        /// <summary>
        /// Loại huy hiệu (Gold, Silver, Bronze)
        /// </summary>
        [MaxLength(50)]
        public string? Type { get; set; }
        
        /// <summary>
        /// Điều kiện để đạt được huy hiệu (dạng chuỗi JSON hoặc mô tả)
        /// </summary>
        public string? Criteria { get; set; }
        
        /// <summary>
        /// Đường dẫn đến hình ảnh của huy hiệu
        /// </summary>
        [MaxLength(255)]
        public string? ImageUrl { get; set; }
        
        /// <summary>
        /// Số điểm danh tiếng được thưởng khi đạt huy hiệu
        /// </summary>
        public int ReputationBonus { get; set; }
        
        /// <summary>
        /// Ngày tạo huy hiệu
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        /// <summary>
        /// Trạng thái huy hiệu (Active, Inactive, etc.)
        /// </summary>
        [MaxLength(50)]
        public string Status { get; set; } = "Active";
        
        /// <summary>
        /// Danh sách các người dùng được gán huy hiệu này
        /// </summary>
        public virtual ICollection<BadgeAssignment> BadgeAssignments { get; set; } = new List<BadgeAssignment>();
    }
} 