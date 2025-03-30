/**
 * Attachment Viewer - Handles direct viewing of file attachments
 * Supports viewing images, PDFs, text files, and other common formats
 */

document.addEventListener('DOMContentLoaded', function() {
    console.log("Attachment Viewer: Initializing...");
    
    // Initialize attachment viewers for all attachment items
    initAttachmentViewers();
    
    // Add event listeners to preview buttons that already exist in the DOM
    const previewButtons = document.querySelectorAll('.preview-file');
    console.log(`Attachment Viewer: Found ${previewButtons.length} preview buttons in the DOM`);
    
    previewButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            e.preventDefault();
            const filePath = this.getAttribute('data-file-path');
            const fileName = this.getAttribute('data-file-name');
            const fileType = this.getAttribute('data-file-type');
            
            console.log(`Attachment Viewer: Preview requested for ${fileName} (${fileType})`);
            
            if (filePath && fileName && fileType) {
                openFilePreview(filePath, fileName, fileType);
            } else {
                console.error('Missing file information for preview:', { filePath, fileName, fileType });
            }
        });
    });
    
    console.log("Attachment Viewer: Initialization complete");
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
        case 'video':
            addVideoPreview(contentContainer, filePath, fileName);
            break;
        case 'audio':
            addAudioPreview(contentContainer, filePath, fileName);
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
 * Adds a PDF preview to the container using PDF.js if available, fallback to standard methods
 * @param {HTMLElement} container - Container to add the preview to
 * @param {string} filePath - Path to the PDF file
 */
function addPdfPreview(container, filePath) {
    console.log('Adding PDF preview for:', filePath);
    container.innerHTML = '';
    
    // Create toolbar with more clear options
    const toolbar = document.createElement('div');
    toolbar.className = 'pdf-toolbar d-flex justify-content-between align-items-center mb-3 p-3 rounded bg-light';
    toolbar.innerHTML = `
        <div class="pdf-info">
            <i class="bi bi-file-earmark-pdf text-danger fs-4 me-2"></i>
            <span class="fw-medium">PDF Document</span>
        </div>
        <div class="btn-group">
            <a href="${filePath}" download class="btn btn-primary">
                <i class="bi bi-download me-1"></i> Download
            </a>
            <a href="${filePath}" target="_blank" class="btn btn-info ms-2">
                <i class="bi bi-box-arrow-up-right me-1"></i> Open in New Tab
            </a>
            <button type="button" class="btn btn-success ms-2" id="directPdfOpen">
                <i class="bi bi-eye me-1"></i> View in New Window
            </button>
        </div>
    `;
    container.appendChild(toolbar);
    
    // Add direct open handler
    const directOpenBtn = toolbar.querySelector('#directPdfOpen');
    directOpenBtn.addEventListener('click', function() {
        console.log('Opening PDF in new window:', filePath);
        window.open(filePath, '_blank', 'width=800,height=600');
    });
    
    // Create a canvas container for PDF.js rendering
    const canvasContainer = document.createElement('div');
    canvasContainer.className = 'pdf-canvas-container';
    canvasContainer.style.height = '500px';
    canvasContainer.style.overflow = 'auto';
    canvasContainer.style.border = '1px solid #dee2e6';
    canvasContainer.style.borderRadius = '4px';
    canvasContainer.style.backgroundColor = '#f8f9fa';
    canvasContainer.style.padding = '15px';
    container.appendChild(canvasContainer);
    
    // Add loading indicator
    const loadingIndicator = document.createElement('div');
    loadingIndicator.className = 'text-center py-5';
    loadingIndicator.innerHTML = `
        <div class="spinner-border text-primary" role="status">
            <span class="visually-hidden">Loading PDF...</span>
        </div>
        <p class="mt-3">Loading PDF document...</p>
    `;
    canvasContainer.appendChild(loadingIndicator);
    
    // Add status message area
    const statusMessage = document.createElement('div');
    statusMessage.className = 'pdf-status-message alert alert-info mt-3';
    statusMessage.style.display = 'none';
    container.appendChild(statusMessage);
    
    // Check if PDF.js is available
    if (typeof pdfjsLib !== 'undefined') {
        console.log('Using PDF.js to render the PDF document');
        
        // Load the PDF document with PDF.js
        pdfjsLib.getDocument(filePath).promise
            .then(function(pdfDoc) {
                console.log('PDF document loaded successfully');
                // Clear the loading indicator
                canvasContainer.innerHTML = '';
                
                // Create a page navigation control
                const pageControls = document.createElement('div');
                pageControls.className = 'pdf-page-controls d-flex justify-content-between align-items-center p-2 bg-light rounded mb-3';
                pageControls.innerHTML = `
                    <div class="page-navigation">
                        <button class="btn btn-sm btn-secondary prev-page" disabled>
                            <i class="bi bi-chevron-left"></i> Previous
                        </button>
                        <span class="mx-2">Page <span class="current-page">1</span> of <span class="total-pages">${pdfDoc.numPages}</span></span>
                        <button class="btn btn-sm btn-secondary next-page">
                            Next <i class="bi bi-chevron-right"></i>
                        </button>
                    </div>
                    <div class="zoom-controls">
                        <button class="btn btn-sm btn-outline-secondary zoom-out">
                            <i class="bi bi-dash-circle"></i>
                        </button>
                        <span class="mx-2 zoom-level">100%</span>
                        <button class="btn btn-sm btn-outline-secondary zoom-in">
                            <i class="bi bi-plus-circle"></i>
                        </button>
                    </div>
                `;
                canvasContainer.appendChild(pageControls);
                
                // Create canvas element for the PDF
                const pdfCanvas = document.createElement('canvas');
                pdfCanvas.className = 'pdf-canvas';
                pdfCanvas.style.display = 'block';
                pdfCanvas.style.margin = '0 auto';
                canvasContainer.appendChild(pdfCanvas);
                
                // Track current state
                let currentPage = 1;
                let currentZoom = 1.0;
                
                // Function to render a specific page
                function renderPage(pageNum) {
                    pdfDoc.getPage(pageNum).then(function(page) {
                        console.log(`Rendering page ${pageNum}`);
                        
                        const viewport = page.getViewport({ scale: currentZoom });
                        pdfCanvas.height = viewport.height;
                        pdfCanvas.width = viewport.width;
                        
                        const renderContext = {
                            canvasContext: pdfCanvas.getContext('2d'),
                            viewport: viewport
                        };
                        
                        page.render(renderContext).promise
                            .then(function() {
                                console.log(`Page ${pageNum} rendered successfully`);
                                updatePageControls();
                            })
                            .catch(function(error) {
                                console.error('Error rendering PDF page:', error);
                                statusMessage.innerHTML = `<i class="bi bi-exclamation-triangle-fill me-2"></i> Error rendering page ${pageNum}: ${error.message}`;
                                statusMessage.className = 'alert alert-danger mt-3';
                                statusMessage.style.display = 'block';
                            });
                    });
                }
                
                // Function to update page navigation controls
                function updatePageControls() {
                    const prevButton = pageControls.querySelector('.prev-page');
                    const nextButton = pageControls.querySelector('.next-page');
                    const currentPageSpan = pageControls.querySelector('.current-page');
                    const zoomLevelSpan = pageControls.querySelector('.zoom-level');
                    
                    prevButton.disabled = currentPage <= 1;
                    nextButton.disabled = currentPage >= pdfDoc.numPages;
                    currentPageSpan.textContent = currentPage;
                    zoomLevelSpan.textContent = `${Math.round(currentZoom * 100)}%`;
                }
                
                // Set up event listeners for navigation
                const prevButton = pageControls.querySelector('.prev-page');
                const nextButton = pageControls.querySelector('.next-page');
                const zoomInButton = pageControls.querySelector('.zoom-in');
                const zoomOutButton = pageControls.querySelector('.zoom-out');
                
                prevButton.addEventListener('click', function() {
                    if (currentPage > 1) {
                        currentPage--;
                        renderPage(currentPage);
                    }
                });
                
                nextButton.addEventListener('click', function() {
                    if (currentPage < pdfDoc.numPages) {
                        currentPage++;
                        renderPage(currentPage);
                    }
                });
                
                zoomInButton.addEventListener('click', function() {
                    currentZoom += 0.25;
                    if (currentZoom > 3) currentZoom = 3; // Max zoom
                    renderPage(currentPage);
                });
                
                zoomOutButton.addEventListener('click', function() {
                    currentZoom -= 0.25;
                    if (currentZoom < 0.5) currentZoom = 0.5; // Min zoom
                    renderPage(currentPage);
                });
                
                // Render the first page
                renderPage(currentPage);
            })
            .catch(function(error) {
                console.error('Error loading PDF document with PDF.js:', error);
                // Show error and fallback to standard methods
                statusMessage.innerHTML = `
                    <i class="bi bi-exclamation-triangle-fill me-2"></i>
                    Could not load PDF with the enhanced viewer: ${error.message}. 
                    Please use the buttons above to view or download.
                `;
                statusMessage.className = 'alert alert-warning mt-3';
                statusMessage.style.display = 'block';
                
                // Clear the loading indicator
                canvasContainer.innerHTML = '';
                
                // Add fallback iframe as a last resort
                tryFallbackIframe(canvasContainer, filePath);
            });
    } else {
        console.warn('PDF.js not available, using fallback methods');
        statusMessage.innerHTML = `
            <i class="bi bi-info-circle me-2"></i>
            Enhanced PDF viewer not available. Using standard preview method.
        `;
        statusMessage.className = 'alert alert-info mt-3';
        statusMessage.style.display = 'block';
        
        // Clear the loading indicator
        canvasContainer.innerHTML = '';
        
        // Use fallback iframe
        tryFallbackIframe(canvasContainer, filePath);
    }
}

/**
 * Tries to create a fallback iframe for PDF viewing when PDF.js is not available
 * @param {HTMLElement} container - Container to add the iframe to
 * @param {string} filePath - Path to the PDF file
 */
function tryFallbackIframe(container, filePath) {
    try {
        // Check if on mobile device
        if (/Android|iPhone|iPad|iPod|Opera Mini/i.test(navigator.userAgent)) {
            container.innerHTML = `
                <div class="alert alert-info mb-0">
                    <i class="bi bi-info-circle me-2"></i>
                    PDF preview may not work on all mobile devices. Please use the "Open in New Tab" or "Download" options.
                </div>`;
            return;
        }
        
        // Create iframe element
        const iframe = document.createElement('iframe');
        iframe.className = 'pdf-preview';
        iframe.src = filePath;
        iframe.style.width = '100%';
        iframe.style.height = '100%';
        iframe.style.border = 'none';
        iframe.setAttribute('type', 'application/pdf');
        
        // Add to container
        container.appendChild(iframe);
        
        // Add message for when iframe fails
        iframe.onerror = function() {
            container.innerHTML = `
                <div class="alert alert-warning mb-0">
            <i class="bi bi-exclamation-triangle-fill me-2"></i>
            PDF preview is not available in your browser. Please use the buttons above to view or download the file.
                </div>`;
        };
    } catch (error) {
        console.error('Error creating fallback iframe:', error);
        container.innerHTML = `
            <div class="alert alert-warning mb-0">
                <i class="bi bi-exclamation-triangle-fill me-2"></i>
                PDF preview is not available. Please use the buttons above to view or download the file.
            </div>`;
    }
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
 * Adds a video preview to the container
 * @param {HTMLElement} container - Container to add the preview to
 * @param {string} filePath - Path to the video file
 * @param {string} fileName - Name of the video file
 */
function addVideoPreview(container, filePath, fileName) {
    container.innerHTML = '';
    
    // Create toolbar for the video viewer
    const toolbar = document.createElement('div');
    toolbar.className = 'video-toolbar d-flex justify-content-between align-items-center mb-2 bg-light p-2 rounded';
    toolbar.innerHTML = `
        <div>
            <a href="${filePath}" download class="btn btn-sm btn-primary">
                <i class="bi bi-download"></i> Download
            </a>
        </div>
        <div class="video-error-message text-danger" style="display: none;">
            <i class="bi bi-exclamation-triangle-fill"></i> 
            Video format not supported in this browser. Please download to view.
        </div>
    `;
    container.appendChild(toolbar);
    
    // Create video element
    const video = document.createElement('video');
    video.className = 'video-preview w-100';
    video.controls = true;
    video.preload = 'metadata';
    video.autoplay = false;
    
    // Add source
    const source = document.createElement('source');
    source.src = filePath;
    source.type = getVideoMimeType(fileName);
    video.appendChild(source);
    
    // Add fallback text
    video.innerHTML += 'Your browser does not support the video tag.';
    
    // Add error handler
    video.addEventListener('error', function() {
        toolbar.querySelector('.video-error-message').style.display = 'block';
    });
    
    container.appendChild(video);
}

/**
 * Adds an audio preview to the container
 * @param {HTMLElement} container - Container to add the preview to
 * @param {string} filePath - Path to the audio file
 * @param {string} fileName - Name of the audio file
 */
function addAudioPreview(container, filePath, fileName) {
    container.innerHTML = '';
    
    // Create toolbar for the audio player
    const toolbar = document.createElement('div');
    toolbar.className = 'audio-toolbar d-flex justify-content-between align-items-center mb-2 bg-light p-2 rounded';
    toolbar.innerHTML = `
        <div>
            <a href="${filePath}" download class="btn btn-sm btn-primary">
                <i class="bi bi-download"></i> Download
            </a>
        </div>
        <div class="audio-error-message text-danger" style="display: none;">
            <i class="bi bi-exclamation-triangle-fill"></i> 
            Audio format not supported in this browser. Please download to listen.
        </div>
    `;
    container.appendChild(toolbar);
    
    // Create audio container with visualization
    const audioContainer = document.createElement('div');
    audioContainer.className = 'audio-container p-3 bg-light rounded';
    
    // Create audio element
    const audio = document.createElement('audio');
    audio.className = 'audio-preview w-100';
    audio.controls = true;
    audio.preload = 'metadata';
    audio.autoplay = false;
    
    // Add source
    const source = document.createElement('source');
    source.src = filePath;
    source.type = getAudioMimeType(fileName);
    audio.appendChild(source);
    
    // Add fallback text
    audio.innerHTML += 'Your browser does not support the audio tag.';
    
    // Add error handler
    audio.addEventListener('error', function() {
        toolbar.querySelector('.audio-error-message').style.display = 'block';
    });
    
    // Add to container
    audioContainer.appendChild(audio);
    container.appendChild(audioContainer);
}

/**
 * Gets the MIME type for a video file based on its extension
 * @param {string} fileName - Name of the video file
 * @returns {string} - MIME type of the video
 */
function getVideoMimeType(fileName) {
    const extension = fileName.split('.').pop().toLowerCase();
    
    switch (extension) {
        case 'mp4':
            return 'video/mp4';
        case 'webm':
            return 'video/webm';
        case 'ogg':
            return 'video/ogg';
        case 'mov':
            return 'video/quicktime';
        default:
            return 'video/' + extension;
    }
}

/**
 * Gets the MIME type for an audio file based on its extension
 * @param {string} fileName - Name of the audio file
 * @returns {string} - MIME type of the audio
 */
function getAudioMimeType(fileName) {
    const extension = fileName.split('.').pop().toLowerCase();
    
    switch (extension) {
        case 'mp3':
            return 'audio/mpeg';
        case 'wav':
            return 'audio/wav';
        case 'ogg':
            return 'audio/ogg';
        case 'm4a':
            return 'audio/m4a';
        default:
            return 'audio/' + extension;
    }
}

/**
 * Determines if a file type is supported for preview
 * @param {string} fileType - Type of the file
 * @returns {boolean} - Whether the file type is supported for preview
 */
function isPreviewSupported(fileType) {
    return ['image', 'pdf', 'text', 'video', 'audio'].includes(fileType);
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
        
        .pdf-preview, .video-preview {
            border: none;
            width: 100%;
            background-color: #f8f9fa;
            border-radius: 4px;
        }
        
        .text-preview {
            max-height: 500px;
            overflow-y: auto;
            white-space: pre-wrap;
            word-break: break-all;
            font-family: monospace;
            background-color: #f8f9fa;
            padding: 16px;
            border-radius: 4px;
        }
        
        .img-fluid {
            max-height: 70vh;
            display: block;
            margin: 0 auto;
            border-radius: 4px;
        }
        
        .audio-container {
            border-radius: 4px;
            padding: 16px;
            background-color: #f8f9fa;
        }
        
        .audio-preview {
            width: 100%;
        }
        
        .modal-dialog.modal-lg {
            max-width: 90vw;
        }
        
        @media (min-width: 992px) {
            .modal-dialog.modal-lg {
                max-width: 800px;
            }
        }
        
        .pdf-toolbar, .video-toolbar, .audio-toolbar {
            border-radius: 4px;
            margin-bottom: 12px;
        }
    `;
    document.head.appendChild(style);
}

// Add styles when the script loads
addAttachmentViewerStyles();