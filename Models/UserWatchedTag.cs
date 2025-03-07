using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnWeb.Models
{
    public class UserWatchedTag
    {
        [Key]
        public int UserWatchedTagId { get; set; }
        
        public int UserId { get; set; }
        
        public int TagId { get; set; }
        
        public DateTime CreatedDate { get; set; }
        
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
        
        [ForeignKey("TagId")]
        public virtual Tag Tag { get; set; }
    }
}