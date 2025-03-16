// Complete Markdown Editor with enhanced file upload and screenshot pasting functionality

document.addEventListener('DOMContentLoaded', function() {
    // Check if we're on a page with a markdown editor
    const markdownTextarea = document.getElementById('Body');
    if (!markdownTextarea) return;
    
    // Add the markdown editor toolbar
    setupMarkdownEditor(markdownTextarea);
    
    // Setup file upload functionality
    setupFileUpload();
    
    // Setup paste functionality for screenshots
    setupPasteHandler();
});

/**
 * Sets up the Markdown editor toolbar and functionality
 * @param {HTMLTextAreaElement} textarea - The textarea element to transform into a Markdown editor
 */
function setupMarkdownEditor(textarea) {
    // Create toolbar container
    const toolbarContainer = document.createElement('div');
    toolbarContainer.className = 'markdown-toolbar border rounded p-2 mb-2 bg-light';
    textarea.parentNode.insertBefore(toolbarContainer, textarea);
    
    // Define toolbar buttons with improved formatting options
    const toolbarButtons = [
        { icon: 'bi-type-h1', title: 'Heading 1', action: () => insertMarkdown('# ', '', 'Heading 1') },
        { icon: 'bi-type-h2', title: 'Heading 2', action: () => insertMarkdown('## ', '', 'Heading 2') },
        { icon: 'bi-type-h3', title: 'Heading 3', action: () => insertMarkdown('### ', '', 'Heading 3') },
        { icon: 'bi-type-bold', title: 'Bold (Ctrl+B)', action: () => insertMarkdown('**', '**', 'Bold text'), shortcut: 'b' },
        { icon: 'bi-type-italic', title: 'Italic (Ctrl+I)', action: () => insertMarkdown('*', '*', 'Italic text'), shortcut: 'i' },
        { icon: 'bi-code', title: 'Inline Code (Ctrl+K)', action: () => insertMarkdown('`', '`', 'code'), shortcut: 'k' },
        { icon: 'bi-code-square', title: 'Code Block', action: () => insertCodeBlock() },
        { icon: 'bi-link', title: 'Link (Ctrl+L)', action: () => insertMarkdown('[', '](https://example.com)', 'Link text'), shortcut: 'l' },
        { icon: 'bi-list-ul', title: 'Bullet List', action: () => insertList('- ') },
        { icon: 'bi-list-ol', title: 'Numbered List', action: () => insertList('1. ') },
        { icon: 'bi-blockquote-left', title: 'Quote', action: () => insertMarkdown('> ', '', 'Quote') },
        { icon: 'bi-table', title: 'Table', action: () => insertTable() },
        { icon: 'bi-image', title: 'Upload Image', action: () => document.getElementById('image-upload').click() },
        { icon: 'bi-paperclip', title: 'Attach File', action: () => document.getElementById('document-upload').click() }
    ];
    
    // Create toolbar buttons
    toolbarButtons.forEach(button => {
        const btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'btn btn-sm btn-outline-secondary me-1';
        btn.title = button.title;
        btn.innerHTML = `<i class="bi ${button.icon}"></i>`;
        btn.addEventListener('click', button.action);
        
        // Add keyboard shortcuts
        if (button.shortcut) {
            document.addEventListener('keydown', function(e) {
                if (e.ctrlKey && e.key.toLowerCase() === button.shortcut && document.activeElement === textarea) {
                    e.preventDefault();
                    button.action();
                }
            });
        }
        
        toolbarContainer.appendChild(btn);
    });
    
    // Add preview button and container
    const previewBtn = document.createElement('button');
    previewBtn.type = 'button';
    previewBtn.className = 'btn btn-sm btn-outline-primary ms-2';
    previewBtn.innerHTML = '<i class="bi bi-eye"></i> Preview';
    previewBtn.addEventListener('click', togglePreview);
    toolbarContainer.appendChild(previewBtn);
    
    // Create preview container
    const previewContainer = document.createElement('div');
    previewContainer.className = 'markdown-preview border rounded p-3 mb-3 d-none';
    previewContainer.style.minHeight = '200px';
    textarea.parentNode.insertBefore(previewContainer, textarea.nextSibling);
    
    // Add classes to textarea
    textarea.classList.add('markdown-input');
    
    // Add syntax highlighting indicator
    const syntaxIndicator = document.createElement('div');
    syntaxIndicator.className = 'syntax-indicator text-muted small mt-1';
    syntaxIndicator.innerHTML = '<i class="bi bi-markdown"></i> Markdown syntax highlighting enabled';
    textarea.parentNode.insertBefore(syntaxIndicator, textarea.nextSibling);
    
    /**
     * Toggles between edit mode and preview mode
     */
    function togglePreview() {
        if (previewContainer.classList.contains('d-none')) {
            // Show preview
            previewContainer.classList.remove('d-none');
            textarea.classList.add('d-none');
            previewBtn.innerHTML = '<i class="bi bi-pencil"></i> Edit';
            
            // Convert markdown to HTML using marked.js
            const markdownText = textarea.value;
            const html = convertMarkdownToHtml(markdownText);
            previewContainer.innerHTML = html;
            
            // Add syntax highlighting to code blocks
            highlightCodeBlocks();
        } else {
            // Show editor
            previewContainer.classList.add('d-none');
            textarea.classList.remove('d-none');
            previewBtn.innerHTML = '<i class="bi bi-eye"></i> Preview';
        }
    }
    
    /**
     * Highlights code blocks in the preview using Prism.js if available
     */
    function highlightCodeBlocks() {
        // If Prism.js is available, use it to highlight code blocks
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
    }
    
    /**
     * Inserts a code block with language selection dropdown
     */
    function insertCodeBlock() {
        const languages = ['', 'javascript', 'html', 'css', 'csharp', 'sql', 'python', 'java'];
        
        // Create language selector dropdown
        const dropdown = document.createElement('div');
        dropdown.className = 'code-language-dropdown position-absolute bg-white border rounded p-2 shadow';
        dropdown.style.zIndex = '1000';
        
        // Position dropdown near cursor
        const rect = textarea.getBoundingClientRect();
        dropdown.style.top = (textarea.offsetTop + 30) + 'px';
        dropdown.style.left = (textarea.offsetLeft + 10) + 'px';
        
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
     * Inserts a markdown table template at the cursor position
     */
    function insertTable() {
        const tableTemplate = 
`| Header 1 | Header 2 | Header 3 |
| -------- | -------- | -------- |
| Cell 1   | Cell 2   | Cell 3   |
| Cell 4   | Cell 5   | Cell 6   |`;
        
        insertAtCursor(textarea, tableTemplate);
    }
    
    /**
     * Inserts a list at the current cursor position
     * @param {string} prefix - The list item prefix (e.g., '- ' or '1. ')
     */
    function insertList(prefix) {
        const start = textarea.selectionStart;
        const end = textarea.selectionEnd;
        const text = textarea.value;
        const selection = text.substring(start, end);
        
        // If there's a selection, convert each line to a list item
        if (selection) {
            const lines = selection.split('\n');
            const listItems = lines.map(line => prefix + line).join('\n');
            
            textarea.value = text.substring(0, start) + listItems + text.substring(end);
            textarea.focus();
            textarea.setSelectionRange(start + listItems.length, start + listItems.length);
        } else {
            // If no selection, just insert a single list item
            insertMarkdown(prefix, '', 'List item');
        }
    }
    
    /**
     * Inserts markdown syntax at the current cursor position
     * @param {string} before - Text to insert before the selection
     * @param {string} after - Text to insert after the selection
     * @param {string} placeholder - Default text to use if no text is selected
     */
    function insertMarkdown(before, after, placeholder) {
        const start = textarea.selectionStart;
        const end = textarea.selectionEnd;
        const text = textarea.value;
        const selection = text.substring(start, end) || placeholder;
        
        const replacement = before + selection + after;
        textarea.value = text.substring(0, start) + replacement + text.substring(end);
        
        // Set cursor position
        const newCursorPos = start + before.length + selection.length;
        textarea.focus();
        textarea.setSelectionRange(newCursorPos, newCursorPos);
    }
}

/**
 * Sets up file upload functionality for the markdown editor
 * Handles both image and document uploads
 */
function setupFileUpload() {
    // Create file input for images
    const imageInput = document.createElement('input');
    imageInput.type = 'file';
    imageInput.id = 'image-upload';
    imageInput.className = 'd-none';
    imageInput.accept = 'image/*';
    imageInput.multiple = true;
    document.querySelector('form').appendChild(imageInput);
    
    // Create file input for documents
    const docInput = document.createElement('input');
    docInput.type = 'file';
    docInput.id = 'document-upload';
    docInput.name = 'Attachments';
    docInput.className = 'form-control mt-3';
    docInput.multiple = true;
    docInput.accept = '.pdf,.doc,.docx,.txt,.zip,.rar';
    
    // Create document upload section with improved UI
    const docSection = document.createElement('div');
    docSection.className = 'mb-3 attachment-section';
    docSection.innerHTML = `
        <label class="form-label"><i class="bi bi-paperclip me-1"></i>Attachments (Documents, PDFs, etc.)</label>
        <div id="attachment-list" class="mb-2"></div>
        <div class="small text-muted mb-2">Supported formats: PDF, DOC, DOCX, TXT, ZIP, RAR (Max 10MB per file)</div>
    `;
    docSection.appendChild(docInput);
    
    // Add document section after the Body textarea
    const textarea = document.getElementById('Body');
    textarea.parentNode.insertBefore(docSection, textarea.nextSibling.nextSibling);
    
    // Add drag and drop zone
    const dropZone = document.createElement('div');
    dropZone.className = 'drop-zone border border-dashed rounded p-4 text-center mb-3 d-none';
    dropZone.innerHTML = `
        <i class="bi bi-cloud-upload fs-3"></i>
        <p class="mb-0">Drag and drop files here or click to upload</p>
    `;
    docSection.insertBefore(dropZone, docInput);
    
    // Show drop zone when dragging files over the document
    document.addEventListener('dragover', function(e) {
        e.preventDefault();
        dropZone.classList.remove('d-none');
    });
    
    document.addEventListener('dragleave', function(e) {
        if (!e.relatedTarget || !dropZone.contains(e.relatedTarget)) {
            dropZone.classList.add('d-none');
        }
    });
    
    // Handle file drop
    dropZone.addEventListener('drop', function(e) {
        e.preventDefault();
        dropZone.classList.add('d-none');
        
        const files = e.dataTransfer.files;
        if (!files || files.length === 0) return;
        
        // Process dropped files
        for (let i = 0; i < files.length; i++) {
            const file = files[i];
            if (file.type.startsWith('image/')) {
                // Handle image files
                uploadImage(file);
            } else {
                // Handle document files
                // Add to document input files
                const dataTransfer = new DataTransfer();
                
                // Add existing files
                if (docInput.files) {
                    for (let j = 0; j < docInput.files.length; j++) {
                        dataTransfer.items.add(docInput.files[j]);
                    }
                }
                
                // Add new file
                dataTransfer.items.add(file);
                docInput.files = dataTransfer.files;
                
                // Trigger change event to update UI
                const event = new Event('change');
                docInput.dispatchEvent(event);
            }
        }
    });
    
    // Handle image upload
    imageInput.addEventListener('change', function(e) {
        const files = e.target.files;
        if (!files || files.length === 0) return;
        
        for (let i = 0; i < files.length; i++) {
            uploadImage(files[i]);
        }
    });
    
    // Handle document upload display with improved UI
    docInput.addEventListener('change', function(e) {
        const files = e.target.files;
        const attachmentList = document.getElementById('attachment-list');
        attachmentList.innerHTML = '';
        
        if (!files || files.length === 0) return;
        
        for (let i = 0; i < files.length; i++) {
            const file = files[i];
            const fileItem = document.createElement('div');
            fileItem.className = 'attachment-item p-2 border rounded mb-1 d-flex justify-content-between align-items-center';
            
            // Determine file icon based on type
            let fileIcon = 'bi-file-earmark';
            if (file.name.endsWith('.pdf')) fileIcon = 'bi-file-earmark-pdf';
            else if (file.name.endsWith('.doc') || file.name.endsWith('.docx')) fileIcon = 'bi-file-earmark-word';
            else if (file.name.endsWith('.zip') || file.name.endsWith('.rar')) fileIcon = 'bi-file-earmark-zip';
            
            fileItem.innerHTML = `
                <span><i class="bi ${fileIcon} me-2"></i> ${file.name} (${formatFileSize(file.size)})</span>
                <button type="button" class="btn btn-sm btn-outline-danger remove-file" data-index="${i}">
                    <i class="bi bi-x"></i>
                </button>
            `;
            attachmentList.appendChild(fileItem);
        }
        
        // Add event listeners to remove buttons
        document.querySelectorAll('.remove-file').forEach(button => {
            button.addEventListener('click', function() {
                const index = parseInt(this.getAttribute('data-index'));
                removeFile(docInput, index);
            });
        });
    });
}

/**
 * Removes a file from the input's FileList at the specified index
 * @param {HTMLInputElement} input - The file input element
 * @param {number} index - The index of the file to remove
 */
function removeFile(input, index) {
    // Create a new FileList without the removed file
    const dt = new DataTransfer();
    const files = input.files;
    
    for (let i = 0; i < files.length; i++) {
        if (i !== index) {
            dt.items.add(files[i]);
        }
    }
    
    input.files = dt.files;
    
    // Trigger change event to update UI
    const event = new Event('change');
    input.dispatchEvent(event);
}

/**
 * Sets up paste handler for direct screenshot pasting into the editor
 * This allows users to paste images directly from clipboard
 */
function setupPasteHandler() {
    const textarea = document.getElementById('Body');
    
    // Handle paste event for screenshots
    document.addEventListener('paste', function(e) {
        if (document.activeElement !== textarea) return;
        
        const items = e.clipboardData.items;
        for (let i = 0; i < items.length; i++) {
            if (items[i].type.indexOf('image') !== -1) {
                // Prevent default paste behavior
                e.preventDefault();
                
                // Get the image file
                const file = items[i].getAsFile();
                uploadImage(file);
                break;
            }
        }
    });
    
    // Add paste indicator
    const pasteIndicator = document.createElement('div');
    pasteIndicator.className = 'paste-indicator text-muted small mt-1';
    pasteIndicator.innerHTML = '<i class="bi bi-clipboard-check me-1"></i>You can paste screenshots directly into the editor';
    textarea.parentNode.appendChild(pasteIndicator);
}

/**
 * Uploads an image file to the server and inserts the markdown at cursor position
 * @param {File} file - The image file to upload
 */
function uploadImage(file) {
    // Create a loading indicator
    const textarea = document.getElementById('Body');
    const loadingText = `![Uploading ${file.name}...]()`;
    insertAtCursor(textarea, loadingText);
    
    // Show preview immediately before uploading
    const reader = new FileReader();
    reader.onload = function(e) {
        // Create a temporary preview
        const tempPreview = document.createElement('div');
        tempPreview.className = 'temp-image-preview mt-2 mb-2 border rounded p-2';
        tempPreview.innerHTML = `
            <div class="d-flex align-items-center">
                <div class="me-2"><i class="bi bi-image"></i> ${file.name}</div>
                <div class="flex-grow-1">
                    <div class="progress" style="height: 6px;">
                        <div class="progress-bar progress-bar-striped progress-bar-animated" style="width: 100%"></div>
                    </div>
                </div>
            </div>
            <div class="mt-2">
                <img src="${e.target.result}" class="img-fluid img-thumbnail" style="max-height: 200px;" alt="${file.name}">
            </div>
        `;
        
        // Insert after the textarea
        textarea.parentNode.insertBefore(tempPreview, textarea.nextSibling);
    };
    reader.readAsDataURL(file);
    
    // Create FormData
    const formData = new FormData();
    formData.append('image', file);
    
    // Send to server
    fetch('/api/upload/image', {
        method: 'POST',
        body: formData
    })
    .then(response => {
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }
        return response.json();
    })
    .then(data => {
        // Replace loading text with actual image markdown
        const imageMarkdown = `![${file.name}](${data.imageUrl})`;
        textarea.value = textarea.value.replace(loadingText, imageMarkdown);
        
        // Remove temporary preview
        const tempPreviews = document.querySelectorAll('.temp-image-preview');
        tempPreviews.forEach(preview => preview.remove());
        
        // Store the URL in the form
        addUploadedImageUrl(data.imageUrl);
    })
    .catch(error => {
        console.error('Error uploading image:', error);
        // Replace loading text with error message
        textarea.value = textarea.value.replace(loadingText, `<!-- Failed to upload ${file.name}: ${error.message} -->`);
        
        // Remove temporary preview for this file
        const tempPreviews = document.querySelectorAll('.temp-image-preview');
        if (tempPreviews.length > 0) {
            // Find the preview containing this file name and remove it
            tempPreviews.forEach(preview => {
                if (preview.textContent.includes(file.name)) {
                    preview.remove();
                }
            });
        }
        
        // Show error notification
        showNotification('Error uploading image: ' + error.message, 'danger');
    });
}

/**
 * Shows a notification message to the user
 * @param {string} message - The message to display
 * @param {string} type - The type of notification (info, success, warning, danger)
 */
function showNotification(message, type = 'info') {
    const notification = document.createElement('div');
    notification.className = `alert alert-${type} alert-dismissible fade show position-fixed bottom-0 end-0 m-3`;
    notification.setAttribute('role', 'alert');
    notification.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    `;
    document.body.appendChild(notification);
    
    // Auto-dismiss after 5 seconds
    setTimeout(() => {
        notification.classList.remove('show');
        setTimeout(() => notification.remove(), 150);
    }, 5000);
}

/**
 * Adds an uploaded image URL to the form data
 * @param {string} url - The URL of the uploaded image
 */
function addUploadedImageUrl(url) {
    // Create a hidden input to store the uploaded image URL
    let input = document.querySelector('input[name="UploadedImageUrls"]');
    if (!input) {
        input = document.createElement('input');
        input.type = 'hidden';
        input.name = 'UploadedImageUrls';
        document.querySelector('form').appendChild(input);
    }
    
    // Append the URL to the input value
    const currentValue = input.value ? input.value + ',' : '';
    input.value = currentValue + url;
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

/**
 * Converts markdown text to HTML using the marked library if available
 * Falls back to a simple regex-based conversion if marked.js is not available
 * @param {string} markdown - The markdown text to convert
 * @returns {string} The HTML representation of the markdown
 */
function convertMarkdownToHtml(markdown) {
    // If marked.js is available, use it
    if (typeof marked !== 'undefined') {
        return marked.parse(markdown);
    }
    
    // Simple markdown to HTML conversion as fallback
    let html = markdown;
    
    // Convert headers
    html = html.replace(/^### (.+)$/gm, '<h3>$1</h3>');
    html = html.replace(/^## (.+)$/gm, '<h2>$1</h2>');
    html = html.replace(/^# (.+)$/gm, '<h1>$1</h1>');
    
    // Convert bold and italic
    html = html.replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>');
    html = html.replace(/\*(.+?)\*/g, '<em>$1</em>');
    
    // Convert links
    html = html.replace(/\[(.+?)\]\((.+?)\)/g, '<a href="$2">$1</a>');
    
    // Convert images
    html = html.replace(/!\[(.+?)\]\((.+?)\)/g, '<img src="$2" alt="$1" class="img-fluid">');
    
    // Convert code blocks
    html = html.replace(/```([\s\S]+?)```/g, '<pre><code>$1</code></pre>');
    
    // Convert inline code
    html = html.replace(/`(.+?)`/g, '<code>$1</code>');
    
    // Convert lists
    html = html.replace(/^- (.+)$/gm, '<li>$1</li>');
    html = html.replace(/^\d+\. (.+)$/gm, '<li>$1</li>');
    
    // Wrap lists
    html = html.replace(/(<li>.+<\/li>\n)+/g, '<ul>$&</ul>');
    
    // Convert blockquotes
    html = html.replace(/^> (.+)$/gm, '<blockquote>$1</blockquote>');
    
    // Convert paragraphs
    html = html.replace(/^(?!<[a-z]).+$/gm, '<p>$&</p>');
    
    return html;
}

/**
 * Formats a file size in bytes to a human-readable string
 * @param {number} bytes - The file size in bytes
 * @returns {string} The formatted file size (e.g., "2.5 MB")
 */
function formatFileSize(bytes) {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}