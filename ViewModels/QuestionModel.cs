using System;

namespace DoAnWeb.ViewModels
{
    public class Model
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string AuthorId { get; set; } = string.Empty;
    }
}