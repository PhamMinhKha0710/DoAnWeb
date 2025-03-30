namespace DoAnWeb.Models
{
    /// <summary>
    /// Các loại thông báo trong hệ thống
    /// </summary>
    public enum NotificationTypes
    {
        /// <summary>
        /// Thông báo khi có người vote câu hỏi hoặc câu trả lời
        /// </summary>
        Vote,
        
        /// <summary>
        /// Thông báo khi có câu trả lời mới cho câu hỏi
        /// </summary>
        Answer,
        
        /// <summary>
        /// Thông báo khi có bình luận mới
        /// </summary>
        Comment,
        
        /// <summary>
        /// Thông báo khi câu trả lời được chấp nhận
        /// </summary>
        Accept,
        
        /// <summary>
        /// Thông báo khi được nhắc đến (@mention)
        /// </summary>
        Mention,
        
        /// <summary>
        /// Thông báo thay đổi reputation
        /// </summary>
        ReputationChange,
        
        /// <summary>
        /// Thông báo hệ thống
        /// </summary>
        System
    }
} 