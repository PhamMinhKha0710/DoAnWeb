/**
 * Enhanced file attachment handling for answer forms
 * Provides improved UI for file uploads with validation and preview
 */

document.addEventListener('DOMContentLoaded', function() {
    // Check if we're on a page with answer file attachments
    const fileInput = document.querySelector('input[type="file"][name="AnswerAttachments"]');
    if (!fileInput) return;
    
    // Initialize the enhanced file upload UI
    setupEnhancedFileUpload(fileInput);
});

/**
 * Sets up enhanced file upload UI with validation and preview
 * @param {HTMLInputElement} fileInput - The original file input element
 */
function setupEnhancedFileUpload(fileInput) {
    // Create container for the enhanced upload UI
    const container = document.createElement('div');
    container.className = 'enhanced-upload-container';
    
    // Create file list container
    const fileListContainer = document.createElement('div');
    fileListContainer.id = 'answer-attachment-preview';
    fileListContainer.className = 'attachment-preview mt-2';
    
    // Insert the container after the file input
    fileInput.parentNode.insertBefore(container, fileInput.nextSibling);
    container.appendChild(fileListContainer);
    
    // Add drag and drop zone
    const dropZone = document.createElement('div');
    dropZone.className = 'drop-zone border border-dashed rounded p-3 text-center mb-3 d-none';
    dropZone.innerHTML = `
        <i class="bi bi-cloud-upload fs-3"></i>
        <p class="mb-0">Drag and drop files here or click to upload</p>
    `;
    fileInput.parentNode.insertBefore(dropZone, fileInput);
    
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
        
        // Add files to input
        addFilesToInput(fileInput, files);
    });
    
    // Handle file selection
    fileInput.addEventListener('change', function() {
        updateFilePreview(fileInput, fileListContainer);
    });
}

/**
 * Adds files to the file input
 * @param {HTMLInputElement} input - The file input element
 * @param {FileList} newFiles - The new files to add
 */
function addFilesToInput(input, newFiles) {
    // Create a new DataTransfer object
    const dataTransfer = new DataTransfer();
    
    // Add existing files
    if (input.files) {
        for (let i = 0; i < input.files.length; i++) {
            dataTransfer.items.add(input.files[i]);
        }
    }
    
    // Add new files with validation
    for (let i = 0; i < newFiles.length; i++) {
        const file = newFiles[i];
        
        // Validate file type - allow images for answers
        const validTypes = ['.jpg', '.jpeg', '.png', '.gif', '.bmp', '.webp'];
        const fileExtension = '.' + file.name.split('.').pop().toLowerCase();
        
        if (!validTypes.includes(fileExtension)) {
            showNotification(`File type ${fileExtension} is not supported. Please upload JPG, JPEG, PNG, GIF, BMP, or WEBP files.`, 'danger');
            continue;
        }
        
        // Validate file size (5MB max)
        const maxSize = 5 * 1024 * 1024; // 5MB in bytes
        if (file.size > maxSize) {
            showNotification(`File ${file.name} exceeds the maximum size of 5MB.`, 'danger');
            continue;
        }
        
        // Add valid file
        dataTransfer.items.add(file);
    }
    
    // Update the input's files
    input.files = dataTransfer.files;
    
    // Trigger change event to update UI
    const event = new Event('change');
    input.dispatchEvent(event);
}

/**
 * Updates the file preview based on selected files
 * @param {HTMLInputElement} fileInput - The file input element
 * @param {HTMLElement} container - The container for file previews
 */
function updateFilePreview(fileInput, container) {
    container.innerHTML = '';
    
    if (!fileInput.files || fileInput.files.length === 0) {
        return;
    }
    
    // Create preview for each file
    for (let i = 0; i < fileInput.files.length; i++) {
        const file = fileInput.files[i];
        const fileItem = document.createElement('div');
        fileItem.className = 'attachment-item p-2 border rounded mb-2 d-flex align-items-center';
        
        // Determine file icon based on extension
        let fileIcon = 'bi-file-earmark-image';
        
        // Create preview content
        fileItem.innerHTML = `
            <i class="bi ${fileIcon} me-2 fs-5"></i>
            <div class="flex-grow-1">
                <div class="fw-medium">${file.name}</div>
                <div class="text-muted small">${Math.round(file.size / 1024)} KB</div>
            </div>
            <button type="button" class="btn btn-sm btn-outline-danger remove-file" data-index="${i}">
                <i class="bi bi-x"></i>
            </button>
        `;
        
        container.appendChild(fileItem);
        
        // Add image preview for image files
        if (file.type.startsWith('image/')) {
            const reader = new FileReader();
            reader.onload = function(e) {
                const imgPreview = document.createElement('div');
                imgPreview.className = 'mt-2';
                imgPreview.innerHTML = `<img src="${e.target.result}" class="img-thumbnail" style="max-height: 100px;" alt="${file.name}">`;
                fileItem.appendChild(imgPreview);
            };
            reader.readAsDataURL(file);
        }
    }
    
    // Add remove file functionality
    container.querySelectorAll('.remove-file').forEach(button => {
        button.addEventListener('click', function() {
            const index = parseInt(this.getAttribute('data-index'));
            removeFile(fileInput, index);
            updateFilePreview(fileInput, container);
        });
    });
}

/**
 * Removes a file from the file input
 * @param {HTMLInputElement} input - The file input element
 * @param {number} index - The index of the file to remove
 */
function removeFile(input, index) {
    const dataTransfer = new DataTransfer();
    
    for (let i = 0; i < input.files.length; i++) {
        if (i !== index) {
            dataTransfer.items.add(input.files[i]);
        }
    }
    
    input.files = dataTransfer.files;
}

/**
 * Shows a notification message
 * @param {string} message - The message to display
 * @param {string} type - The type of notification (success, danger, etc.)
 */
function showNotification(message, type = 'info') {
    const notification = document.createElement('div');
    notification.className = `alert alert-${type} alert-dismissible fade show`;
    notification.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    `;
    
    // Find a suitable container for the notification
    const container = document.querySelector('.enhanced-upload-container');
    if (container) {
        container.prepend(notification);
    } else {
        // Fallback to body if container not found
        document.body.prepend(notification);
    }
    
    // Auto-dismiss after 5 seconds
    setTimeout(() => {
        notification.classList.remove('show');
        setTimeout(() => notification.remove(), 300);
    }, 5000);
}

/**
 * Add CSS styles for the enhanced upload UI
 */
function addStyles() {
    const style = document.createElement('style');
    style.textContent = `
        .border-dashed {
            border-style: dashed !important;
        }
        
        .drop-zone {
            transition: all 0.3s ease;
            background-color: rgba(0, 123, 255, 0.05);
        }
        
        .drop-zone:hover {
            background-color: rgba(0, 123, 255, 0.1);
            cursor: pointer;
        }
        
        .attachment-preview {
            max-height: 200px;
            overflow-y: auto;
        }
        
        .attachment-item {
            transition: all 0.2s ease;
        }
        
        .attachment-item:hover {
            background-color: rgba(0, 0, 0, 0.03);
        }
    `;
    document.head.appendChild(style);
}

// Add styles when the document is loaded
document.addEventListener('DOMContentLoaded', addStyles);