// Markdown Editor and Image Upload functionality

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

function setupMarkdownEditor(textarea) {
    // Create toolbar container
    const toolbarContainer = document.createElement('div');
    toolbarContainer.className = 'markdown-toolbar border rounded p-2 mb-2 bg-light';
    textarea.parentNode.insertBefore(toolbarContainer, textarea);
    
    // Define toolbar buttons
    const toolbarButtons = [
        { icon: 'bi-type-bold', title: 'Bold', action: () => insertMarkdown('**', '**', 'Bold text') },
        { icon: 'bi-type-italic', title: 'Italic', action: () => insertMarkdown('*', '*', 'Italic text') },
        { icon: 'bi-code', title: 'Code', action: () => insertMarkdown('`', '`', 'code') },
        { icon: 'bi-code-square', title: 'Code Block', action: () => insertMarkdown('```\n', '\n```', 'code block') },
        { icon: 'bi-link', title: 'Link', action: () => insertMarkdown('[', '](https://example.com)', 'Link text') },
        { icon: 'bi-list-ul', title: 'Bullet List', action: () => insertMarkdown('- ', '', 'List item') },
        { icon: 'bi-list-ol', title: 'Numbered List', action: () => insertMarkdown('1. ', '', 'List item') },
        { icon: 'bi-blockquote-left', title: 'Quote', action: () => insertMarkdown('> ', '', 'Quote') },
        { icon: 'bi-image', title: 'Image', action: () => document.getElementById('image-upload').click() }
    ];
    
    // Create toolbar buttons
    toolbarButtons.forEach(button => {
        const btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'btn btn-sm btn-outline-secondary me-1';
        btn.title = button.title;
        btn.innerHTML = `<i class="bi ${button.icon}"></i>`;
        btn.addEventListener('click', button.action);
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
    
    // Function to toggle preview
    function togglePreview() {
        if (previewContainer.classList.contains('d-none')) {
            // Show preview
            previewContainer.classList.remove('d-none');
            textarea.classList.add('d-none');
            previewBtn.innerHTML = '<i class="bi bi-pencil"></i> Edit';
            
            // Convert markdown to HTML
            const markdownText = textarea.value;
            const html = convertMarkdownToHtml(markdownText);
            previewContainer.innerHTML = html;
        } else {
            // Show editor
            previewContainer.classList.add('d-none');
            textarea.classList.remove('d-none');
            previewBtn.innerHTML = '<i class="bi bi-eye"></i> Preview';
        }
    }
    
    // Function to insert markdown
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
    
    // Create document upload section
    const docSection = document.createElement('div');
    docSection.className = 'mb-3';
    docSection.innerHTML = `
        <label class="form-label">Attachments (Documents, PDFs, etc.)</label>
        <div id="attachment-list" class="mb-2"></div>
    `;
    docSection.appendChild(docInput);
    
    // Add document section after the Body textarea
    const textarea = document.getElementById('Body');
    textarea.parentNode.insertBefore(docSection, textarea.nextSibling.nextSibling);
    
    // Handle image upload
    imageInput.addEventListener('change', function(e) {
        const files = e.target.files;
        if (!files || files.length === 0) return;
        
        for (let i = 0; i < files.length; i++) {
            uploadImage(files[i]);
        }
    });
    
    // Handle document upload display
    docInput.addEventListener('change', function(e) {
        const files = e.target.files;
        const attachmentList = document.getElementById('attachment-list');
        attachmentList.innerHTML = '';
        
        if (!files || files.length === 0) return;
        
        for (let i = 0; i < files.length; i++) {
            const file = files[i];
            const fileItem = document.createElement('div');
            fileItem.className = 'attachment-item p-2 border rounded mb-1 d-flex justify-content-between align-items-center';
            fileItem.innerHTML = `
                <span><i class="bi bi-file-earmark"></i> ${file.name} (${formatFileSize(file.size)})</span>
            `;
            attachmentList.appendChild(fileItem);
        }
    });
}

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
}

function uploadImage(file) {
    // Create a loading indicator
    const textarea = document.getElementById('Body');
    const loadingText = `![Uploading ${file.name}...]()`;
    insertAtCursor(textarea, loadingText);
    
    // Create FormData
    const formData = new FormData();
    formData.append('image', file);
    
    // Simulate upload (in a real app, you would send to server)
    // For demo purposes, we'll use a timeout and FileReader to show the image
    setTimeout(() => {
        const reader = new FileReader();
        reader.onload = function(e) {
            // In a real app, this would be the URL returned from the server
            const imageUrl = e.target.result;
            
            // Replace loading text with actual image markdown
            const imageMarkdown = `![${file.name}](${imageUrl})`;
            textarea.value = textarea.value.replace(loadingText, imageMarkdown);
            
            // In a real implementation, you would store the URL in the form
            // to be submitted with the form data
            addUploadedImageUrl(imageUrl);
        };
        reader.readAsDataURL(file);
    }, 1000);
}

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

function convertMarkdownToHtml(markdown) {
    // Simple markdown to HTML conversion
    // In a real app, you would use a library like marked.js
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
    
    // Fix line breaks
    html = html.replace(/\n/g, '');
    
    return html;
}

function formatFileSize(bytes) {
    if (bytes === 0) return '0 Bytes';
    
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}