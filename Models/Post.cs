using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnWeb.Models
{
    public class Post
    {
        [Key]
        public int PostId { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Title { get; set; }
        
        [Required]
        public string Content { get; set; }
        
        [Required]
        public DateTime CreatedDate { get; set; }
        
        public DateTime? UpdatedDate { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [ForeignKey("UserId")]
        public User User { get; set; }
        
        public ICollection<Comment> Comments { get; set; }
        
        // Bổ sung các thuộc tính cần thiết khác
        public string Slug { get; set; }
        
        public bool IsPublished { get; set; } = true;
        
        public bool IsFeatured { get; set; } = false;
        
        public int ViewCount { get; set; } = 0;
    }
} 