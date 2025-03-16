/**
 * Simple Markdown Editor
 * A lightweight Markdown editor with basic formatting options
 */
document.addEventListener('DOMContentLoaded', function() {
    // Check if we're on a page with a markdown editor
    const markdownTextarea = document.getElementById('Body');
    if (!markdownTextarea) return;
    
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
     * This prevents HTML code from being displayed directly
     */
    function cleanupTextareaContent() {
        if (!markdownTextarea) return;
        
        // Check if the content looks like HTML (contains HTML tags)
        const content = markdownTextarea.value;
        if (content.includes('<div') || content.includes('<span') || 
            content.includes('<form') || content.includes('@await')) {
            
            // If it contains Razor syntax or HTML, escape it as code block
            if (content.includes('@await') || content.includes('@{') || content.includes('@model')) {
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
            
            // Convert markdown to HTML
            const markdownText = markdownTextarea.value;
            let html = '';
            
            
            if (typeof marked !== 'undefined') {
                // Configure marked.js options to properly handle code blocks
                marked.setOptions({
                    highlight: function(code, lang) {
                        // If Prism is available, use it for syntax highlighting
                        if (typeof Prism !== 'undefined' && Prism.languages[lang]) {
                            return Prism.highlight(code, Prism.languages[lang], lang);
                        }
                        return code;
                    },
                    breaks: true,
                    gfm: true
                });
                
                // Use marked.js if available
                html = marked.parse(markdownText);
            } else {
                // Simple fallback
                html = markdownText
                    .replace(/^### (.+)$/gm, '<h3>$1</h3>')
                    .replace(/^## (.+)$/gm, '<h2>$1</h2>')
                    .replace(/^# (.+)$/gm, '<h1>$1</h1>')
                    .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
                    .replace(/\*(.+?)\*/g, '<em>$1</em>')
                    .replace(/`(.+?)`/g, '<code>$1</code>')
                    .replace(/```([a-z]*)?\n([\s\S]+?)```/g, function(match, lang, code) {
                        return '<pre><code class="language-' + (lang || 'plaintext') + '">' + code + '</code></pre>';
                    })
                    .replace(/\[(.+?)\]\((.+?)\)/g, '<a href="$2">$1</a>')
                    .replace(/!\[(.+?)\]\((.+?)\)/g, '<img src="$2" alt="$1" class="img-fluid">')
                    .replace(/^- (.+)$/gm, '<li>$1</li>')
                    .replace(/^\d+\. (.+)$/gm, '<li>$1</li>')
                    .replace(/(<li>.+<\/li>\n)+/g, '<ul>$&</ul>')
                    .replace(/^> (.+)$/gm, '<blockquote>$1</blockquote>');
            }
            
            previewContainer.innerHTML = html;
            
            // Add syntax highlighting to code blocks
            if (typeof Prism !== 'undefined') {
                Prism.highlightAllUnder(previewContainer);
            }
            
            // Preserve code formatting for all code blocks
            const codeBlocks = previewContainer.querySelectorAll('pre code, code');
            codeBlocks.forEach(block => {
                // Ensure code blocks maintain their original formatting
                block.style.whiteSpace = 'pre';
                block.style.tabSize = '4';
                block.style.MozTabSize = '4';
                
                // Apply theme-specific styling
                if (document.documentElement.getAttribute('data-theme') === 'dark') {
                    block.style.backgroundColor = '#334155';
                    block.style.color = '#e2e8f0';
                } else {
                    block.style.backgroundColor = '#f1f5f9';
                    block.style.color = '#334155';
                }
            });
            
            // Ensure the submit button and form container remain visible
            if (submitButton) {
                submitButton.setAttribute('style', 'display: inline-block !important; visibility: visible !important; opacity: 1 !important; z-index: 9999 !important; position: relative !important; pointer-events: auto !important; cursor: pointer !important;');
            }
            
            if (formContainer) {
                formContainer.setAttribute('style', 'display: flex !important; visibility: visible !important; opacity: 1 !important; z-index: 9999 !important; position: relative !important;');
            }
        } else {
            // Show editor
            previewContainer.classList.add('d-none');
            markdownTextarea.classList.remove('d-none');
            previewBtn.innerHTML = '<i class="bi bi-eye"></i> Preview';
            
            // Remove preview mode class
            document.body.classList.remove('markdown-preview-active');
            
            // Restore original content if it was stored
            if (markdownTextarea.hasAttribute('data-original-content')) {
                markdownTextarea.value = markdownTextarea.getAttribute('data-original-content');
            }
            
            // Ensure the submit button and form container remain visible
            if (submitButton) {
                submitButton.setAttribute('style', 'display: inline-block !important; visibility: visible !important; opacity: 1 !important; z-index: 9999 !important; position: relative !important; pointer-events: auto !important; cursor: pointer !important;');
            }
            
            if (formContainer) {
                formContainer.setAttribute('style', 'display: flex !important; visibility: visible !important; opacity: 1 !important; z-index: 9999 !important; position: relative !important;');
            }
        }
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
});