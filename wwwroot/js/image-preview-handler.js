/**
 * Image Preview Handler for Question/Answer Attachments
 * 
 * This script handles the functionality for previewing uploaded images
 * before they are submitted with a question or answer.
 */

document.addEventListener('DOMContentLoaded', function() {
    // Initialize lightbox with custom settings
    if (typeof lightbox !== 'undefined') {
        lightbox.option({
            'resizeDuration': 200,
            'wrapAround': true,
            'albumLabel': 'Hình ảnh %1 / %2',
            'fadeDuration': 300,
            'imageFadeDuration': 300,
            'positionFromTop': 80,
            'showImageNumberLabel': true,
            'disableScrolling': true,
            'fitImagesInViewport': true,
            'maxWidth': Math.min(window.innerWidth - 100, 1200),
            'maxHeight': window.innerHeight - 160
        });
    }
    
    // Setup for question attachments preview
    setupImagePreview('file-upload', 'image-preview-container');
    
    // Setup for answer attachments preview
    setupImagePreview('answer-attachments', 'answer-image-preview-container');
});

/**
 * Sets up image preview functionality for file upload inputs
 * 
 * @param {string} inputId - The ID of the file input element
 * @param {string} containerId - The ID of the container where previews will be shown
 */
function setupImagePreview(inputId, containerId) {
    const fileUpload = document.getElementById(inputId);
    const previewContainer = document.getElementById(containerId);
    
    if (!fileUpload || !previewContainer) return;
    
    // Handle file selection
    fileUpload.addEventListener('change', function() {
        // Clear previous previews
        previewContainer.innerHTML = '';
        
        // Process each selected file
        Array.from(this.files).forEach((file, index) => {
            // Check if file is an image
            if (file.type.startsWith('image/')) {
                // Create preview element
                const previewCol = document.createElement('div');
                previewCol.className = 'col-md-4 col-sm-6 mb-3';
                previewCol.setAttribute('data-file-name', file.name);
                
                const previewDiv = document.createElement('div');
                previewDiv.className = 'image-preview';
                
                // Create image container with loading indicator
                const imgContainer = document.createElement('div');
                imgContainer.className = 'position-relative';
                
                // Add loading spinner
                const loadingSpinner = document.createElement('div');
                loadingSpinner.className = 'position-absolute top-50 start-50 translate-middle';
                loadingSpinner.innerHTML = '<div class="spinner-border text-primary" role="status"><span class="visually-hidden">Loading...</span></div>';
                imgContainer.appendChild(loadingSpinner);
                
                // Create image element
                const img = document.createElement('img');
                img.alt = file.name;
                img.style.opacity = '0';
                img.onload = function() {
                    // Remove spinner and show image when loaded
                    loadingSpinner.remove();
                    img.style.opacity = '1';
                    img.style.transition = 'opacity 0.3s ease';
                };
                
                imgContainer.appendChild(img);
                
                // Create file name label
                const fileNameLabel = document.createElement('div');
                fileNameLabel.className = 'small text-center text-truncate px-2 pt-1 pb-2';
                fileNameLabel.title = file.name;
                fileNameLabel.textContent = file.name;
                
                // Create remove button
                const removeBtn = document.createElement('button');
                removeBtn.className = 'remove-preview';
                removeBtn.innerHTML = '<i class="bi bi-x"></i>';
                removeBtn.type = 'button';
                removeBtn.dataset.index = index;
                removeBtn.addEventListener('click', function() {
                    // Animate removal
                    previewCol.style.transition = 'all 0.3s ease';
                    previewCol.style.transform = 'scale(0.8)';
                    previewCol.style.opacity = '0';
                    
                    setTimeout(() => {
                        previewCol.remove();
                        
                        // Create a notification about removal
                        const notification = document.createElement('div');
                        notification.className = 'alert alert-info alert-dismissible fade show mt-2';
                        notification.innerHTML = `
                            <i class="bi bi-info-circle me-2"></i>
                            <strong>${file.name}</strong> will be removed when you submit the form
                            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
                        `;
                        previewContainer.appendChild(notification);
                        
                        // Auto remove notification after 3 seconds
                        setTimeout(() => {
                            notification.classList.remove('show');
                            setTimeout(() => notification.remove(), 150);
                        }, 3000);
                    }, 300);
                });
                
                // Read the file and set the image source
                const reader = new FileReader();
                reader.onload = function(e) {
                    img.src = e.target.result;
                };
                reader.readAsDataURL(file);
                
                // Add elements to the DOM
                previewDiv.appendChild(imgContainer);
                previewDiv.appendChild(fileNameLabel);
                previewDiv.appendChild(removeBtn);
                previewCol.appendChild(previewDiv);
                previewContainer.appendChild(previewCol);
            }
        });
    });
} 