/**
 * Attachment Viewer - Handles direct viewing of file attachments
 * Supports viewing images, PDFs, text files, and other common formats
 */

document.addEventListener('DOMContentLoaded', function() {
    // Initialize attachment viewers for all attachment items
    initAttachmentViewers();
});

/**
 * Initializes attachment viewers for all attachment items on the page
 */
function initAttachmentViewers() {
    // Find all attachment items
    const attachmentItems = document.querySelectorAll('.attachment-item');
    if (!attachmentItems.length) return;
    
    // Add preview button to each attachment item
    attachmentItems.forEach(item => {
        const downloadBtn = item.querySelector('a[download]');
        if (!downloadBtn) return;
        
        const filePath = downloadBtn.getAttribute('href');
        const fileName = downloadBtn.getAttribute('download');
        const fileType = getFileType(fileName);
        
        // Only add preview button for supported file types
        if (isPreviewSupported(fileType)) {
            const previewBtn = createPreviewButton(filePath, fileName, fileType);
            downloadBtn.parentNode.insertBefore(previewBtn, downloadBtn);
        }
    });
}

/**
 * Creates a preview button for an attachment
 * @param {string} filePath - Path to the file
 * @param {string} fileName - Name of the file
 * @param {string} fileType - Type of the file
 * @returns {HTMLElement} - The preview button element
 */
function createPreviewButton(filePath, fileName, fileType) {
    const previewBtn = document.createElement('button');
    previewBtn.className = 'btn btn-sm btn-outline-secondary me-2';
    previewBtn.innerHTML = '<i class="bi bi-eye me-1"></i> View';
    previewBtn.addEventListener('click', function(e) {
        e.preventDefault();
        openFilePreview(filePath, fileName, fileType);
    });
    return previewBtn;
}

/**
 * Opens a preview for the file based on its type
 * @param {string} filePath - Path to the file
 * @param {string} fileName - Name of the file
 * @param {string} fileType - Type of the file
 */
function openFilePreview(filePath, fileName, fileType) {
    // Create modal for preview
    const modal = createPreviewModal(fileName);
    document.body.appendChild(modal);
    
    // Add content based on file type
    const contentContainer = modal.querySelector('.modal-body');
    
    switch (fileType) {
        case 'image':
            addImagePreview(contentContainer, filePath, fileName);
            break;
        case 'pdf':
            addPdfPreview(contentContainer, filePath);
            break;
        case 'text':
            fetchAndDisplayText(contentContainer, filePath);
            break;
        default:
            contentContainer.innerHTML = `<div class="alert alert-info">This file type cannot be previewed directly. Please download the file to view it.</div>`;
    }
    
    // Show the modal
    const bsModal = new bootstrap.Modal(modal);
    bsModal.show();
    
    // Clean up when modal is closed
    modal.addEventListener('hidden.bs.modal', function() {
        modal.remove();
    });
}

/**
 * Creates a modal for file preview
 * @param {string} fileName - Name of the file to preview
 * @returns {HTMLElement} - The modal element
 */
function createPreviewModal(fileName) {
    const modal = document.createElement('div');
    modal.className = 'modal fade';
    modal.id = 'filePreviewModal';
    modal.tabIndex = '-1';
    modal.setAttribute('aria-labelledby', 'filePreviewModalLabel');
    modal.setAttribute('aria-hidden', 'true');
    
    modal.innerHTML = `
        <div class="modal-dialog modal-lg modal-dialog-centered">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title" id="filePreviewModalLabel">${fileName}</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body attachment-preview-container">
                    <div class="text-center">
                        <div class="spinner-border" role="status">
                            <span class="visually-hidden">Loading...</span>
                        </div>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                </div>
            </div>
        </div>
    `;
    
    return modal;
}

/**
 * Adds an image preview to the container
 * @param {HTMLElement} container - Container to add the preview to
 * @param {string} filePath - Path to the image file
 * @param {string} fileName - Name of the image file
 */
function addImagePreview(container, filePath, fileName) {
    container.innerHTML = '';
    const img = document.createElement('img');
    img.className = 'img-fluid';
    img.src = filePath;
    img.alt = fileName;
    container.appendChild(img);
}

/**
 * Adds a PDF preview to the container
 * @param {HTMLElement} container - Container to add the preview to
 * @param {string} filePath - Path to the PDF file
 */
function addPdfPreview(container, filePath) {
    container.innerHTML = '';
    const iframe = document.createElement('iframe');
    iframe.className = 'pdf-preview';
    iframe.src = filePath;
    iframe.width = '100%';
    iframe.height = '500px';
    container.appendChild(iframe);
}

/**
 * Fetches and displays text content
 * @param {HTMLElement} container - Container to add the text to
 * @param {string} filePath - Path to the text file
 */
function fetchAndDisplayText(container, filePath) {
    fetch(filePath)
        .then(response => response.text())
        .then(text => {
            container.innerHTML = '';
            const pre = document.createElement('pre');
            pre.className = 'text-preview p-3 bg-light';
            pre.textContent = text;
            container.appendChild(pre);
        })
        .catch(error => {
            container.innerHTML = `<div class="alert alert-danger">Error loading text file: ${error.message}</div>`;
        });
}

/**
 * Determines if a file type is supported for preview
 * @param {string} fileType - Type of the file
 * @returns {boolean} - Whether the file type is supported for preview
 */
function isPreviewSupported(fileType) {
    return ['image', 'pdf', 'text'].includes(fileType);
}

/**
 * Gets the file type based on file extension
 * @param {string} fileName - Name of the file
 * @returns {string} - Type of the file
 */
function getFileType(fileName) {
    const extension = fileName.split('.').pop().toLowerCase();
    
    // Image files
    if (['jpg', 'jpeg', 'png', 'gif', 'svg', 'webp'].includes(extension)) {
        return 'image';
    }
    
    // PDF files
    if (extension === 'pdf') {
        return 'pdf';
    }
    
    // Text files
    if (['txt', 'log', 'md', 'csv', 'json', 'xml', 'html', 'css', 'js'].includes(extension)) {
        return 'text';
    }
    
    // Default
    return 'other';
}

/**
 * Adds CSS styles for the attachment viewer
 */
function addAttachmentViewerStyles() {
    const style = document.createElement('style');
    style.textContent = `
        .attachment-preview-container {
            min-height: 200px;
        }
        
        .pdf-preview {
            border: none;
        }
        
        .text-preview {
            max-height: 500px;
            overflow-y: auto;
            white-space: pre-wrap;
            word-break: break-all;
        }
        
        .img-fluid {
            max-height: 70vh;
        }
    `;
    document.head.appendChild(style);
}

// Add styles when the script loads
addAttachmentViewerStyles();