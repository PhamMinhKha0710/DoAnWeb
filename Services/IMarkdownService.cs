using System;

namespace DoAnWeb.Services
{
    /// <summary>
    /// Định nghĩa service xử lý chuyển đổi Markdown sang HTML an toàn
    /// </summary>
    public interface IMarkdownService
    {
        /// <summary>
        /// Chuyển đổi nội dung Markdown thành HTML an toàn
        /// </summary>
        /// <param name="markdown">Chuỗi Markdown đầu vào</param>
        /// <returns>Chuỗi HTML an toàn đã được xử lý</returns>
        string ConvertToHtml(string markdown);
    }
} 