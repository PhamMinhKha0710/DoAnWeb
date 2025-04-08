using System.Text.Json.Serialization;

namespace DoAnWeb.Models
{
    /// <summary>
    /// Model representing the progress of a user towards earning a badge
    /// </summary>
    public class BadgeProgress
    {
        /// <summary>
        /// ID of the badge
        /// </summary>
        public int BadgeId { get; set; }
        
        /// <summary>
        /// Name of the badge
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Description of how to earn the badge
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Type/category of the badge (Bronze, Silver, Gold)
        /// </summary>
        public string Type { get; set; } = string.Empty;
        
        /// <summary>
        /// Icon class for the badge (Bootstrap Icons class name)
        /// </summary>
        public string IconClass { get; set; } = "bi-award-fill";
        
        /// <summary>
        /// Background color class for the badge
        /// </summary>
        public string ColorClass { get; set; } = "bg-primary";
        
        /// <summary>
        /// Current progress count towards earning the badge
        /// </summary>
        public int CurrentCount { get; set; }
        
        /// <summary>
        /// Target count required to earn the badge
        /// </summary>
        public int TargetCount { get; set; }
        
        /// <summary>
        /// Whether the user has earned this badge already
        /// </summary>
        public bool IsEarned { get; set; }
        
        /// <summary>
        /// Calculate the percentage progress (0-100)
        /// </summary>
        [JsonIgnore]
        public int ProgressPercentage => TargetCount > 0 
            ? Math.Min(100, (int)Math.Round((double)CurrentCount / TargetCount * 100)) 
            : 0;
            
        /// <summary>
        /// Get the progress text (e.g., "3/5")
        /// </summary>
        [JsonIgnore]
        public string ProgressText => $"{CurrentCount}/{TargetCount}";
    }
} 