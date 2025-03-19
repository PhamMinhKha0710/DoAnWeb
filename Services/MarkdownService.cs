using System;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Ganss.Xss;
using Markdig.Parsers;

namespace DoAnWeb.Services
{
    /// <summary>
    /// Service xử lý chuyển đổi Markdown sang HTML an toàn
    /// </summary>
    public class MarkdownService : IMarkdownService
    {
        private readonly HtmlSanitizer _sanitizer;
        private readonly MarkdownPipeline _pipeline;

        /// <summary>
        /// Constructor khởi tạo MarkdownService với cấu hình mặc định
        /// </summary>
        public MarkdownService()
        {
            // Cấu hình Markdig pipeline với các tính năng cần thiết
            _pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UsePipeTables()
                .UseGridTables() 
                .UseTaskLists()
                .UseEmojiAndSmiley()
                .UseAutoLinks()
                .UseMediaLinks() 
                .UseCustomCodeBlockFormatter() // Tùy chỉnh hiển thị mã nguồn
                .Build();

            // Cấu hình HtmlSanitizer
            _sanitizer = new HtmlSanitizer();
            
            // Cho phép các thẻ và thuộc tính cần thiết cho hiển thị mã
            _sanitizer.AllowedTags.Add("pre");
            _sanitizer.AllowedTags.Add("code");
            _sanitizer.AllowDataAttributes = true;
            
            // Cho phép tất cả các class language-* để hỗ trợ Prism.js syntax highlighting cho mọi ngôn ngữ
            _sanitizer.AllowedClasses.Add("language-csharp");
            _sanitizer.AllowedClasses.Add("language-cs");
            _sanitizer.AllowedClasses.Add("language-javascript");
            _sanitizer.AllowedClasses.Add("language-js");
            _sanitizer.AllowedClasses.Add("language-typescript");
            _sanitizer.AllowedClasses.Add("language-ts");
            _sanitizer.AllowedClasses.Add("language-css");
            _sanitizer.AllowedClasses.Add("language-html");
            _sanitizer.AllowedClasses.Add("language-xml");
            _sanitizer.AllowedClasses.Add("language-sql");
            _sanitizer.AllowedClasses.Add("language-python");
            _sanitizer.AllowedClasses.Add("language-py");
            _sanitizer.AllowedClasses.Add("language-java");
            _sanitizer.AllowedClasses.Add("language-php");
            _sanitizer.AllowedClasses.Add("language-cpp");
            _sanitizer.AllowedClasses.Add("language-c");
            _sanitizer.AllowedClasses.Add("language-ruby");
            _sanitizer.AllowedClasses.Add("language-go");
            _sanitizer.AllowedClasses.Add("language-rust");
            _sanitizer.AllowedClasses.Add("language-kotlin");
            _sanitizer.AllowedClasses.Add("language-swift");
            _sanitizer.AllowedClasses.Add("language-dart");
            _sanitizer.AllowedClasses.Add("language-json");
            _sanitizer.AllowedClasses.Add("language-yaml");
            _sanitizer.AllowedClasses.Add("language-powershell");
            _sanitizer.AllowedClasses.Add("language-bash");
            _sanitizer.AllowedClasses.Add("language-shell");
            _sanitizer.AllowedClasses.Add("language-markdown");
            _sanitizer.AllowedClasses.Add("language-plaintext");
            _sanitizer.AllowedClasses.Add("language-none");
            
            // Cho phép các class bổ sung cho Prism
            _sanitizer.AllowedClasses.Add("line-numbers");
            _sanitizer.AllowedClasses.Add("match-braces");
            _sanitizer.AllowedClasses.Add("line-highlight");

            // Thiết lập một cách tổng quát - chấp nhận tất cả các class bắt đầu bằng "language-"
            _sanitizer.AllowedClasses.Add("language-*");
        }

        /// <summary>
        /// Chuyển đổi nội dung Markdown thành HTML an toàn
        /// </summary>
        /// <param name="markdown">Chuỗi Markdown đầu vào</param>
        /// <returns>Chuỗi HTML an toàn đã được xử lý</returns>
        public string ConvertToHtml(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
                return string.Empty;

            try
            {
                // Chuyển đổi Markdown sang HTML
                string html = Markdown.ToHtml(markdown, _pipeline);
                
                // Làm sạch HTML để ngăn chặn XSS
                string sanitizedHtml = _sanitizer.Sanitize(html);
                
                return sanitizedHtml;
            }
            catch (Exception ex)
            {
                // Xử lý lỗi nếu có
                Console.WriteLine($"Error converting Markdown to HTML: {ex.Message}");
                return $"<p class=\"text-danger\">Error processing content: {ex.Message}</p>";
            }
        }
    }

    /// <summary>
    /// Extension method cho MarkdownPipelineBuilder để tùy chỉnh hiển thị mã nguồn
    /// </summary>
    public static class MarkdownExtensions
    {
        /// <summary>
        /// Tùy chỉnh định dạng hiển thị mã nguồn với các ngôn ngữ hỗ trợ
        /// </summary>
        public static MarkdownPipelineBuilder UseCustomCodeBlockFormatter(this MarkdownPipelineBuilder builder)
        {
            // Thêm xử lý tùy chỉnh cho code blocks
            builder.UseEmphasisExtras()
                   .UseSoftlineBreakAsHardlineBreak()
                   .UseAutoIdentifiers();
                   
            // Thêm hook để tùy chỉnh cách render code blocks
            builder.Extensions.Add(new CustomCodeBlockExtension());
            
            return builder;
        }
    }
    
    /// <summary>
    /// Extension tùy chỉnh để xử lý code blocks
    /// </summary>
    public class CustomCodeBlockExtension : Markdig.IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            // Không cần thiết lập thêm tại đây
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer)
            {
                // Đăng ký renderer tùy chỉnh cho code blocks
                htmlRenderer.ObjectRenderers.AddIfNotAlready<CustomCodeBlockRenderer>();
                htmlRenderer.ObjectRenderers.AddIfNotAlready<CustomFencedCodeBlockRenderer>();
            }
        }
    }

    /// <summary>
    /// Renderer tùy chỉnh cho code blocks thông thường
    /// </summary>
    public class CustomCodeBlockRenderer : HtmlObjectRenderer<CodeBlock>
    {
        protected override void Write(HtmlRenderer renderer, CodeBlock obj)
        {
            // Sử dụng class general cho code blocks không có info string
            renderer.Write("<pre><code class=\"language-plaintext\">");
            renderer.WriteLeafRawLines(obj, true, true);
            renderer.Write("</code></pre>");
        }
    }

    /// <summary>
    /// Renderer tùy chỉnh cho fenced code blocks (```language)
    /// </summary>
    public class CustomFencedCodeBlockRenderer : HtmlObjectRenderer<FencedCodeBlock>
    {
        protected override void Write(HtmlRenderer renderer, FencedCodeBlock obj)
        {
            string languageClass = string.IsNullOrWhiteSpace(obj.Info) ? "language-plaintext" : $"language-{obj.Info.ToLowerInvariant()}";
            
            // Nếu có xác định ngôn ngữ, đặt thuộc tính data-language
            string languageAttr = string.IsNullOrWhiteSpace(obj.Info) ? "" : $" data-language=\"{obj.Info.ToLowerInvariant()}\"";
            
            // Render với class language cho Prism.js
            renderer.Write($"<pre{languageAttr}><code class=\"{languageClass}\">");
            renderer.WriteLeafRawLines(obj, true, true);
            renderer.Write("</code></pre>");
        }
    }
} 