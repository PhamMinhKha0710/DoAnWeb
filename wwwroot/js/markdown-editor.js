/**
 * Simple Markdown Editor
 * A lightweight Markdown editor with basic formatting options
 */
document.addEventListener('DOMContentLoaded', function() {
    // Check if we're on a page with a markdown editor
    const markdownTextarea = document.getElementById('Body');
    if (!markdownTextarea) return;
    
    // Cấu hình Marked.js nếu được tải
    setupMarkedRenderer();
    
    // Create toolbar container
    const toolbarContainer = document.createElement('div');
    toolbarContainer.className = 'markdown-toolbar border rounded p-2 mb-2 bg-light';
    markdownTextarea.parentNode.insertBefore(toolbarContainer, markdownTextarea);
    
    // Define toolbar buttons
    const toolbarButtons = [
        { icon: 'bi-type-h1', title: 'Heading 1', action: () => insertMarkdown('# ', '', 'Heading 1') },
        { icon: 'bi-type-h2', title: 'Heading 2', action: () => insertMarkdown('## ', '', 'Heading 2') },
        { icon: 'bi-type-h3', title: 'Heading 3', action: () => insertMarkdown('### ', '', 'Heading 3') },
        { icon: 'bi-type-bold', title: 'Bold', action: () => insertMarkdown('**', '**', 'Bold text') },
        { icon: 'bi-type-italic', title: 'Italic', action: () => insertMarkdown('*', '*', 'Italic text') },
        { icon: 'bi-code', title: 'Inline Code', action: () => insertMarkdown('`', '`', 'code') },
        { icon: 'bi-code-square', title: 'Code Block', action: () => insertCodeBlock() },
        { icon: 'bi-link', title: 'Link', action: () => insertMarkdown('[', '](https://example.com)', 'Link text') },
        { icon: 'bi-list-ul', title: 'Bullet List', action: () => insertMarkdown('- ', '', 'List item') },
        { icon: 'bi-list-ol', title: 'Numbered List', action: () => insertMarkdown('1. ', '', 'List item') },
        { icon: 'bi-blockquote-left', title: 'Quote', action: () => insertMarkdown('> ', '', 'Quote') },
        { icon: 'bi-table', title: 'Table', action: () => insertTable() }
    ];
    
    // Create toolbar buttons
    toolbarButtons.forEach(button => {
        const btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'btn btn-sm btn-outline-secondary me-1 markdown-toolbar-btn';
        btn.title = button.title;
        btn.innerHTML = `<i class="bi ${button.icon}"></i>`;
        btn.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            button.action();
            return false;
        });
        toolbarContainer.appendChild(btn);
    });
    
    // Add preview button
    const previewBtn = document.createElement('button');
    previewBtn.type = 'button';
    previewBtn.className = 'btn btn-sm btn-outline-primary ms-2 markdown-preview-btn';
    previewBtn.innerHTML = '<i class="bi bi-eye"></i> Preview';
    previewBtn.addEventListener('click', function(e) {
        e.preventDefault();
        e.stopPropagation();
        togglePreview();
        return false;
    });
    toolbarContainer.appendChild(previewBtn);
    
    // Create preview container
    const previewContainer = document.createElement('div');
    previewContainer.className = 'markdown-preview border rounded p-3 mb-3 d-none';
    previewContainer.style.minHeight = '200px';
    previewContainer.style.zIndex = '5';
    markdownTextarea.parentNode.insertBefore(previewContainer, markdownTextarea.nextSibling);
    
    // Add syntax highlighting indicator
    const syntaxIndicator = document.createElement('div');
    syntaxIndicator.className = 'syntax-indicator text-muted small mt-1';
    syntaxIndicator.innerHTML = '<i class="bi bi-markdown"></i> Markdown syntax highlighting enabled';
    markdownTextarea.parentNode.insertBefore(syntaxIndicator, previewContainer.nextSibling);
    
    // Clean up any HTML content in the textarea
    cleanupTextareaContent();
    
    /**
     * Cleans up any HTML content in the textarea
     * This prevents HTML code from being displayed directly by converting it to a code block if necessary
     */
    function cleanupTextareaContent() {
        if (!markdownTextarea) return;
        
        // Lấy nội dung hiện tại của textarea
        const content = markdownTextarea.value;
        
        // Nếu đã là code block, không cần xử lý
        if (content.startsWith('```') && content.endsWith('```')) {
            return;
        }
        
        // Kiểm tra xem có phải là HTML không
        const isHtml = content.trim().startsWith('<!DOCTYPE') || 
                      content.trim().startsWith('<html') ||
                      content.includes('@model') ||
                      content.includes('@using') ||
                      content.includes('@addTagHelper') ||
                      (content.includes('<') && content.includes('</') && 
                        (content.includes('<div') || content.includes('<span') || 
                         content.includes('<p>') || content.includes('<h1>') || 
                         content.includes('<ul>') || content.includes('<a ') ||
                         content.includes('<head') || content.includes('<body')));
        
        // Nếu là Razor syntax hoặc HTML, chuyển đổi thành code block
        if (isHtml) {
            if (content.includes('@await') || content.includes('@{') || content.includes('@model') || content.includes('@using')) {
                markdownTextarea.value = '```cshtml\n' + content + '\n```';
            } else if (content.match(/<[a-z][\s\S]*>/i)) {
                markdownTextarea.value = '```html\n' + content + '\n```';
            }
        }
    }
    
    /**
     * Toggles between edit mode and preview mode
     */
    function togglePreview() {
        // Find the submit button and form container to ensure they stay visible
        const submitButton = document.getElementById('submit-button');
        const formContainer = document.querySelector('.d-grid.gap-2.d-md-flex.justify-content-md-end');
        
        if (previewContainer.classList.contains('d-none')) {
            // Show preview
            previewContainer.classList.remove('d-none');
            markdownTextarea.classList.add('d-none');
            previewBtn.innerHTML = '<i class="bi bi-pencil"></i> Edit';
            
            // Add a class to indicate preview mode is active
            document.body.classList.add('markdown-preview-active');
            
            // Store original markdown text to preserve formatting
            markdownTextarea.setAttribute('data-original-content', markdownTextarea.value);
            
            // Lưu trữ nội dung gốc (không qua xử lý) để đảm bảo không bị mất định dạng
            const rawContent = markdownTextarea.value;
            
            try {
                // Kiểm tra và xử lý trước nếu là HTML hoặc Razor
                let processedContent = preprocessCodeBlocks(rawContent);
                
                // Kiểm tra nếu là HTML thuần túy hoặc Razor chưa được bọc trong code block
                const isRawHtml = (
                    rawContent.trim().startsWith('<!DOCTYPE') || 
                    rawContent.trim().startsWith('<html') ||
                    rawContent.includes('@model') ||
                    rawContent.includes('@using') ||
                    rawContent.includes('@addTagHelper') ||
                    (rawContent.includes('<') && rawContent.includes('</') && 
                     (rawContent.includes('<div') || rawContent.includes('<span') || 
                      rawContent.includes('<p>') || rawContent.includes('<h1>') || 
                      rawContent.includes('<ul>') || rawContent.includes('<a') ||
                      rawContent.includes('<head') || rawContent.includes('<body')))
                ) && !rawContent.trim().startsWith('```');
                
                if (isRawHtml) {
                    // Tự động bọc HTML trong code block nếu chưa được bọc
                    if (rawContent.includes('@model') || rawContent.includes('@using') || 
                        rawContent.includes('@{') || rawContent.includes('@addTagHelper')) {
                        processedContent = '```cshtml\n' + rawContent + '\n```';
                    } else {
                        processedContent = '```html\n' + rawContent + '\n```';
                    }
                }
                
                // Convert markdown to HTML using marked.js
                let html = '';
                if (typeof marked !== 'undefined') {
                    marked.setOptions({
                        breaks: true,
                        gfm: true,
                        pedantic: false,
                        smartLists: true,
                        smartypants: false,
                        xhtml: false
                    });
                    html = marked.parse(processedContent);
                } else {
                    // Fallback nếu marked.js không có sẵn
                    html = processedContent
                        .replace(/</g, '&lt;')
                        .replace(/>/g, '&gt;')
                        .replace(/```(\w*)\n([\s\S]+?)```/g, function(match, lang, code) {
                            return '<pre><code class="language-' + lang + '">' + code + '</code></pre>';
                        });
                }
                
                // Create a temporary container to process the HTML
                const tempContainer = document.createElement('div');
                tempContainer.innerHTML = html;
                tempContainer.className = 'markdown-preview';
                
                // Apply special processing to code blocks
                postProcessCodeBlocks(tempContainer);
                
                // Update the preview with the processed HTML
                previewContainer.innerHTML = tempContainer.innerHTML;
                
                // If there's a submit button, keep it visible in preview mode
                if (submitButton && formContainer) {
                    const clonedFormContainer = formContainer.cloneNode(true);
                    previewContainer.appendChild(clonedFormContainer);
                }
                
                // Kích hoạt syntax highlighting nếu Prism có sẵn
                if (typeof Prism !== 'undefined') {
                    Prism.highlightAllUnder(previewContainer);
                }
            } catch (error) {
                console.error('Error in markdown preview:', error);
                previewContainer.innerHTML = '<div class="alert alert-danger">Error rendering preview: ' + error.message + '</div>';
                previewContainer.innerHTML += '<pre class="border p-3 bg-light">' + rawContent.replace(/</g, '&lt;').replace(/>/g, '&gt;') + '</pre>';
            }
        } else {
            // Switch back to edit mode
            previewContainer.classList.add('d-none');
            markdownTextarea.classList.remove('d-none');
            previewBtn.innerHTML = '<i class="bi bi-eye"></i> Preview';
            
            // Remove the preview active class
            document.body.classList.remove('markdown-preview-active');
            
            // Restore the original content to prevent formatting loss
            const originalContent = markdownTextarea.getAttribute('data-original-content');
            if (originalContent) {
                markdownTextarea.value = originalContent;
            }
        }
    }
    
    /**
     * Xử lý trước các code block để bảo toàn định dạng
     */
    function preprocessCodeBlocks(markdown) {
        if (!markdown) return markdown;
        
        // Kiểm tra xem có phải là HTML hoặc Razor không
        if (markdown.trim().startsWith('<!DOCTYPE') || 
            markdown.trim().startsWith('<html') ||
            markdown.includes('@model') ||
            markdown.includes('@using') ||
            markdown.includes('@addTagHelper') ||
            (markdown.includes('<') && markdown.includes('</') && 
              (markdown.includes('<div') || markdown.includes('<span') || 
               markdown.includes('<p>') || markdown.includes('<h1>') || 
               markdown.includes('<ul>') || markdown.includes('<a') ||
               markdown.includes('<head') || markdown.includes('<body')))
           ) {
            // Nếu là HTML hoặc Razor, bọc nó trong code block thích hợp nếu chưa được bọc
            if (!markdown.trim().startsWith('```')) {
                if (markdown.includes('@model') || markdown.includes('@using') || 
                    markdown.includes('@{') || markdown.includes('@addTagHelper')) {
                    return '```cshtml\n' + markdown + '\n```';
                } else {
                    return '```html\n' + markdown + '\n```';
                }
            }
        }
        
        // Giữ nguyên ký tự khoảng trắng và tab trong code blocks
        return markdown.replace(/```([a-z]*)?(\r?\n)([\s\S]*?)```/g, (match, lang, newline, code) => {
            // Đảm bảo giữ nguyên định dạng code
            return '```' + (lang || '') + newline + code + '```';
        });
    }
    
    /**
     * Xử lý sau các code block để đảm bảo định dạng hiển thị đúng
     */
    function postProcessCodeBlocks(container) {
        const codeBlocks = container.querySelectorAll('pre code');
        codeBlocks.forEach(block => {
            // Đảm bảo code blocks giữ nguyên định dạng
            block.style.whiteSpace = 'pre';
            block.style.tabSize = '4';
            block.style.MozTabSize = '4';
            block.style.OTabSize = '4';
            block.style.display = 'block';
            block.style.overflowX = 'auto';
            block.style.wordBreak = 'keep-all';
            block.style.wordWrap = 'normal';
            block.style.overflowWrap = 'normal';
            block.style.padding = '0';
            
            // Thêm class cho PrismJS nếu chưa có
            if (!block.classList.contains('language-')) {
                block.classList.add('language-plaintext');
            }
            
            // Xử lý đặc biệt cho HTML và CSHTML code blocks
            if (block.classList.contains('language-html') || block.classList.contains('language-cshtml')) {
                // Đảm bảo tất cả các ký tự đặc biệt được escape đúng
                if (!block.getAttribute('processed-html')) {
                    const content = block.innerHTML;
                    // Kiểm tra xem HTML đã được escape chưa
                    if (content.indexOf('&lt;') === -1 && content.indexOf('<') !== -1) {
                        block.innerHTML = content
                            .replace(/</g, '&lt;')
                            .replace(/>/g, '&gt;');
                    }
                    block.setAttribute('processed-html', 'true');
                }
            }
            
            // Thêm attribut data-preserve-whitespace để CSS có thể nhận diện
            block.setAttribute('data-preserve-whitespace', 'true');
            
            // Đặt thuộc tính quan trọng
            const parent = block.parentElement;
            if (parent && parent.tagName.toLowerCase() === 'pre') {
                parent.style.whiteSpace = 'pre';
                parent.style.tabSize = '4';
                parent.style.overflow = 'auto';
                
                // Thêm thuộc tính data-language nếu có thể
                const langMatch = block.className.match(/language-(\w+)/);
                if (langMatch && langMatch[1] && langMatch[1] !== 'plaintext') {
                    parent.setAttribute('data-language', langMatch[1]);
                }
                
                // Đảm bảo các thuộc tính CSS quan trọng được áp dụng
                parent.style.cssText += '; white-space: pre !important; tab-size: 4 !important; -moz-tab-size: 4 !important; -o-tab-size: 4 !important; overflow-x: auto !important; word-break: normal !important; word-wrap: normal !important; overflow-wrap: normal !important;';
                block.style.cssText += '; white-space: pre !important; tab-size: 4 !important; -moz-tab-size: 4 !important; -o-tab-size: 4 !important; word-break: normal !important; word-wrap: normal !important; overflow-wrap: normal !important;';
            }
        });
    }
    
    /**
     * Inserts a markdown table template at the cursor position
     */
    function insertTable() {
        const tableTemplate = 
`| Header 1 | Header 2 | Header 3 |
| -------- | -------- | -------- |
| Cell 1   | Cell 2   | Cell 3   |
| Cell 4   | Cell 5   | Cell 6   |`;
        
        insertAtCursor(markdownTextarea, tableTemplate);
    }
    
    /**
     * Inserts a code block with language selection
     */
    function insertCodeBlock() {
        const languages = ['', 'javascript', 'html', 'css', 'csharp', 'sql', 'python', 'java'];
        
        // Create language selector dropdown
        const dropdown = document.createElement('div');
        dropdown.className = 'code-language-dropdown position-absolute bg-white border rounded p-2 shadow';
        dropdown.style.zIndex = '1000';
        
        // Position dropdown near cursor
        const rect = markdownTextarea.getBoundingClientRect();
        dropdown.style.top = (markdownTextarea.offsetTop + 30) + 'px';
        dropdown.style.left = (markdownTextarea.offsetLeft + 10) + 'px';
        
        // Add language options
        languages.forEach(lang => {
            const option = document.createElement('div');
            option.className = 'dropdown-item p-1 cursor-pointer hover-bg-light';
            option.textContent = lang || 'Plain text';
            option.addEventListener('click', function() {
                const langPrefix = lang ? `\`\`\`${lang}\n` : '```\n';
                insertMarkdown(langPrefix, '\n```', 'code block');
                document.body.removeChild(dropdown);
            });
            dropdown.appendChild(option);
        });
        
        // Add dropdown to body
        document.body.appendChild(dropdown);
        
        // Remove dropdown when clicking outside
        document.addEventListener('click', function removeDropdown(e) {
            if (!dropdown.contains(e.target)) {
                if (document.body.contains(dropdown)) {
                    document.body.removeChild(dropdown);
                }
                document.removeEventListener('click', removeDropdown);
            }
        });
    }
    
    /**
     * Inserts markdown syntax at the current cursor position
     * @param {string} before - Text to insert before the selection
     * @param {string} after - Text to insert after the selection
     * @param {string} placeholder - Default text to use if no text is selected
     */
    function insertMarkdown(before, after, placeholder) {
        const start = markdownTextarea.selectionStart;
        const end = markdownTextarea.selectionEnd;
        const text = markdownTextarea.value;
        const selection = text.substring(start, end) || placeholder;
        
        const replacement = before + selection + after;
        markdownTextarea.value = text.substring(0, start) + replacement + text.substring(end);
        
        // Set cursor position
        const newCursorPos = start + before.length + selection.length;
        markdownTextarea.focus();
        markdownTextarea.setSelectionRange(newCursorPos, newCursorPos);
    }
    
    /**
     * Helper function to insert text at the current cursor position in a textarea
     * @param {HTMLTextAreaElement} textarea - The textarea element
     * @param {string} text - The text to insert
     */
    function insertAtCursor(textarea, text) {
        const start = textarea.selectionStart;
        const end = textarea.selectionEnd;
        const currentValue = textarea.value;
        
        textarea.value = currentValue.substring(0, start) + text + currentValue.substring(end);
        
        // Set cursor position after inserted text
        const newPosition = start + text.length;
        textarea.setSelectionRange(newPosition, newPosition);
        textarea.focus();
    }
    
    // Add dark mode support
    function updateEditorTheme() {
        const isDarkMode = document.documentElement.getAttribute('data-theme') === 'dark';
        
        if (isDarkMode) {
            toolbarContainer.classList.remove('bg-light');
            toolbarContainer.classList.add('bg-dark', 'text-light');
            
            document.querySelectorAll('.markdown-toolbar .btn-outline-secondary').forEach(btn => {
                btn.classList.add('btn-dark');
            });
        } else {
            toolbarContainer.classList.add('bg-light');
            toolbarContainer.classList.remove('bg-dark', 'text-light');
            
            document.querySelectorAll('.markdown-toolbar .btn-outline-secondary').forEach(btn => {
                btn.classList.remove('btn-dark');
            });
        }
    }
    
    // Initial theme setup
    updateEditorTheme();
    
    // Listen for theme changes
    const observer = new MutationObserver(function(mutations) {
        mutations.forEach(function(mutation) {
            if (mutation.attributeName === 'data-theme') {
                updateEditorTheme();
            }
        });
    });
    
    observer.observe(document.documentElement, { attributes: true });
    
    // Make sure the form can be submitted
    const form = markdownTextarea.closest('form');
    if (form) {
        // Ensure the form can be submitted
        form.addEventListener('submit', function(e) {
            // Make sure the textarea is visible before submitting
            if (markdownTextarea.classList.contains('d-none')) {
                markdownTextarea.classList.remove('d-none');
                previewContainer.classList.add('d-none');
                
                // Restore original content if it was stored
                if (markdownTextarea.hasAttribute('data-original-content')) {
                    markdownTextarea.value = markdownTextarea.getAttribute('data-original-content');
                }
            }
        });
        
        // Make sure the submit button is visible and clickable
        const submitButton = form.querySelector('button[type="submit"]');
        if (submitButton) {
            submitButton.style.display = 'inline-block';
            submitButton.style.visibility = 'visible';
            submitButton.style.opacity = '1';
            submitButton.style.zIndex = '9999';
            submitButton.style.position = 'relative';
            submitButton.style.pointerEvents = 'auto';
            submitButton.style.cursor = 'pointer';
            
            // Force the button to be visible with !important
            submitButton.setAttribute('style', 'display: inline-block !important; visibility: visible !important; opacity: 1 !important; z-index: 9999 !important; position: relative !important; pointer-events: auto !important; cursor: pointer !important;');
        }
    }
    
    // Fix for Razor syntax rendering
    markdownTextarea.addEventListener('input', function() {
        // Check if the content contains Razor syntax or HTML tags
        const content = markdownTextarea.value;
        if (content.includes('@await') || content.includes('@{') || 
            content.includes('@model') || content.includes('<div') || 
            content.includes('<form')) {
            
            // Add a class to indicate special content
            markdownTextarea.classList.add('contains-code');
        } else {
            markdownTextarea.classList.remove('contains-code');
        }
    });
    
    // Initial check for Razor syntax
    if (markdownTextarea.value.includes('@await') || 
        markdownTextarea.value.includes('@{') || 
        markdownTextarea.value.includes('@model')) {
        cleanupTextareaContent();
    }
    
    /**
     * Cấu hình Marked.js Renderer để xử lý đặc biệt các code block
     */
    function setupMarkedRenderer() {
        if (typeof marked === 'undefined') return;
        
        // Tạo một renderer tùy chỉnh
        const renderer = new marked.Renderer();
        
        // Ghi đè phương thức code để xử lý code block đặc biệt
        renderer.code = function(code, language) {
            // Đảm bảo xuống dòng và khoảng trắng được giữ nguyên
            const escapedCode = code
                .replace(/&/g, '&amp;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;')
                .replace(/"/g, '&quot;')
                .replace(/'/g, '&#39;');
            
            const languageClass = language ? `language-${language}` : 'language-plaintext';
            const dataAttr = language ? ` data-language="${language}"` : '';
            
            return `<pre${dataAttr}><code class="${languageClass}" style="white-space: pre !important; tab-size: 4 !important; -moz-tab-size: 4 !important; -o-tab-size: 4 !important; word-break: keep-all !important; word-wrap: normal !important; overflow-wrap: normal !important;">${escapedCode}</code></pre>`;
        };
        
        // Áp dụng cấu hình cho Marked
        marked.setOptions({
            renderer: renderer,
            highlight: function(code, lang) {
                // If Prism is available, use it for syntax highlighting
                if (typeof Prism !== 'undefined' && Prism.languages[lang]) {
                    return Prism.highlight(code, Prism.languages[lang], lang);
                }
                return code;
            },
            breaks: true,
            gfm: true,
            pedantic: false,
            smartLists: true,
            smartypants: false,
            xhtml: false
        });
    }
});