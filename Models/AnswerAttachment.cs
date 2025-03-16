using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnWeb.Models
{
    public class AnswerAttachment
    {
        [Key]
        public int AttachmentId { get; set; }

        [Required]
        public int AnswerId { get; set; }

        [Required]
        [StringLength(255)]
        public required string FileName { get; set; }

        [Required]
        [StringLength(100)]
        public required string ContentType { get; set; }

        [Required]
        [StringLength(255)]
        public required string FilePath { get; set; }

        public long FileSize { get; set; }

        public DateTime UploadDate { get; set; }

        [ForeignKey("AnswerId")]
        public required Answer Answer { get; set; }
    }
}