/**
 * Enhanced file attachment handling for question forms
 * Provides improved UI for file uploads with validation and preview
 */

document.addEventListener('DOMContentLoaded', function() {
    // Check if we're on a page with file attachments
    const fileInput = document.querySelector('input[type="file"][name="Attachments"]');
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
    fileListContainer.id = 'attachment-preview';
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
        
        // Validate file type
        const validTypes = ['.pdf', '.doc', '.docx', '.txt', '.zip', '.rar'];
        const fileExtension = '.' + file.name.split('.').pop().toLowerCase();
        
        if (!validTypes.includes(fileExtension)) {
            showNotification(`File type ${fileExtension} is not supported. Please upload PDF, DOC, DOCX, TXT, ZIP, or RAR files.`, 'danger');
            continue;
        }
        
        // Validate file size (10MB max)
        const maxSize = 10 * 1024 * 1024; // 10MB in bytes
        if (file.size > maxSize) {
            showNotification(`File ${file.name} exceeds the maximum size of 10MB.`, 'danger');
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
 * Updates the file preview list
 * @param {HTMLInputElement} input - The file input element
 * @param {HTMLElement} container - The container for the file preview
 */
function updateFilePreview(input, container) {
    container.innerHTML = '';
    
    if (!input.files || input.files.length === 0) {
        container.style.display = 'none';
        return;
    }
    
    container.style.display = 'block';
    
    // Create file list
    for (let i = 0; i < input.files.length; i++) {
        const file = input.files[i];
        const fileItem = document.createElement('div');
        fileItem.className = 'attachment-item p-2 border rounded mb-1 d-flex justify-content-between align-items-center';
        
        // Determine file icon based on type
        let fileIcon = 'bi-file-earmark';
        if (file.name.endsWith('.pdf')) fileIcon = 'bi-file-earmark-pdf';
        else if (file.name.endsWith('.doc') || file.name.endsWith('.docx')) fileIcon = 'bi-file-earmark-word';
        else if (file.name.endsWith('.zip') || file.name.endsWith('.rar')) fileIcon = 'bi-file-earmark-zip';
        else if (file.name.endsWith('.txt')) fileIcon = 'bi-file-earmark-text';
        
        fileItem.innerHTML = `
            <span><i class="bi ${fileIcon} me-2"></i> ${file.name} (${formatFileSize(file.size)})</span>
            <button type="button" class="btn btn-sm btn-outline-danger remove-file" data-index="${i}">
                <i class="bi bi-x"></i>
            </button>
        `;
        container.appendChild(fileItem);
    }
    
    // Add event listeners to remove buttons
    container.querySelectorAll('.remove-file').forEach(button => {
        button.addEventListener('click', function() {
            const index = parseInt(this.getAttribute('data-index'));
            removeFile(input, index);
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
 * Formats file size in a human-readable format
 * @param {number} bytes - The file size in bytes
 * @returns {string} - Formatted file size
 */
function formatFileSize(bytes) {
    if (bytes === 0) return '0 Bytes';
    
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

/**
 * Shows a notification message
 * @param {string} message - The message to display
 * @param {string} type - The type of notification (success, danger, warning, info)
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
 * Adds CSS styles for the enhanced file upload UI
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

// Add styles when the script loads
addStyles();