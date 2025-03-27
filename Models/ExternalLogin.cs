using System;

namespace DoAnWeb.Models
{
    public class ExternalLogin
    {
        public int ExternalLoginId { get; set; }
        
        public int UserId { get; set; }
        
        public string Provider { get; set; } = null!;
        
        public string ProviderKey { get; set; } = null!;
        
        public string ProviderDisplayName { get; set; } = null!;
        
        public DateTime CreatedDate { get; set; }
        
        public virtual User User { get; set; } = null!;
    }
} 