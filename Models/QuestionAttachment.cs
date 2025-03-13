 using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnWeb.Models
{
    public class QuestionAttachment
    {
        [Key]
        public int AttachmentId { get; set; }

        [Required]
        public int QuestionId { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; }

        [Required]
        [StringLength(100)]
        public string ContentType { get; set; }

        [Required]
        [StringLength(255)]
        public string FilePath { get; set; }

        public long FileSize { get; set; }

        public DateTime UploadDate { get; set; }

        [ForeignKey("QuestionId")]
        public virtual Question Question { get; set; }
    }
}