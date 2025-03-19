/**
 * Comment form handling - Facebook style
 */
const CommentUI = {
    /**
     * Initialize comment forms
     */
    init: function() {
        console.log("Initializing Facebook-style comment UI...");
        
        // Add comment forms to questions and answers
        this.addCommentFormsToPage();
        
        // Setup event delegation for comment forms
        document.addEventListener('click', (event) => {
            // Handle click on "Add a comment" links
            if (event.target.matches('.add-comment-link') || event.target.closest('.add-comment-link')) {
                event.preventDefault();
                const link = event.target.matches('.add-comment-link') ? event.target : event.target.closest('.add-comment-link');
                this.showCommentForm(link);
            }
            
            // Handle comment submit buttons
            if (event.target.matches('.comment-submit-btn') || event.target.closest('.comment-submit-btn')) {
                event.preventDefault();
                const button = event.target.matches('.comment-submit-btn') ? event.target : event.target.closest('.comment-submit-btn');
                this.handleCommentSubmit(button);
            }
            
            // Handle pressing Enter in comment textarea (like Facebook)
            if (event.target.matches('.comment-text')) {
                event.target.addEventListener('keypress', (e) => {
                    if (e.key === 'Enter' && !e.shiftKey) {
                        e.preventDefault();
                        const submitBtn = e.target.closest('form').querySelector('.comment-submit-btn');
                        if (submitBtn) {
                            this.handleCommentSubmit(submitBtn);
                        }
                    }
                });
            }
        });
        
        // Auto-resize textareas as user types (like Facebook)
        document.addEventListener('input', (event) => {
            if (event.target.matches('.comment-text')) {
                this.autoResizeTextarea(event.target);
            }
        });
    },
    
    /**
     * Auto-resize textarea height as user types
     */
    autoResizeTextarea: function(textarea) {
        textarea.style.height = '36px'; // Reset height to default
        textarea.style.height = textarea.scrollHeight + 'px';
    },
    
    /**
     * Add comment forms to questions and answers on the page
     */
    addCommentFormsToPage: function() {
        // Add to question if on question detail page
        const questionDetail = document.querySelector('.question-detail-card');
        if (questionDetail) {
            const questionId = questionDetail.querySelector('[name="id"]')?.value;
            if (questionId) {
                // Add comment form to question
                this.addCommentFormTo('Question', questionId);
                
                // Add comment forms to all answers
                const answers = document.querySelectorAll('.answer-card');
                answers.forEach(answer => {
                    const answerId = answer.querySelector('[name="answerId"]')?.value;
                    if (answerId) {
                        this.addCommentFormTo('Answer', answerId);
                    }
                });
            }
        }
    },
    
    /**
     * Add a comment form to a question or answer
     */
    addCommentFormTo: function(targetType, targetId) {
        // Find the appropriate container
        let container;
        if (targetType === 'Question') {
            container = document.querySelector(`.question-detail-card .question-comments-section`);
        } else if (targetType === 'Answer') {
            container = document.querySelector(`.answer-card [name="answerId"][value="${targetId}"]`)?.closest('.answer-card')?.querySelector('.answer-comments-section');
        }
        
        if (!container) return;
        
        // Check if the comment link already exists
        if (container.querySelector('.add-comment-link')) return;
        
        // Get current user avatar if available
        let currentUserAvatar = document.querySelector('.navbar .nav-item.dropdown img')?.src || '/images/default-avatar.png';
        
        // Create comment link and form container
        const commentLinkHtml = `
            <div class="comment-controls">
                <a href="#" class="add-comment-link" 
                   data-target-type="${targetType}" data-target-id="${targetId}">
                    <i class="bi bi-chat"></i> Viết bình luận...
                </a>
                <div class="comment-form-container d-none">
                    <img src="${currentUserAvatar}" class="current-user-avatar" alt="Your avatar">
                    <form class="comment-form">
                        <div class="form-group">
                            <textarea class="form-control comment-text" 
                                  placeholder="Viết bình luận..."></textarea>
                        </div>
                        <button type="submit" class="btn btn-primary comment-submit-btn">
                            <i class="bi bi-send-fill"></i>
                        </button>
                        <input type="hidden" name="targetType" value="${targetType}">
                        <input type="hidden" name="targetId" value="${targetId}">
                    </form>
                </div>
            </div>
        `;
        
        // Add to the container
        container.insertAdjacentHTML('beforeend', commentLinkHtml);
    },
    
    /**
     * Show the comment form when user clicks "Add a comment"
     */
    showCommentForm: function(link) {
        const container = link.parentNode.querySelector('.comment-form-container');
        
        if (!container) return;
        
        // Hide the "Add a comment" link
        link.classList.add('d-none');
        
        // Show the form container
        container.classList.remove('d-none');
        
        // Focus the textarea
        const textarea = container.querySelector('.comment-text');
        if (textarea) textarea.focus();
    },
    
    /**
     * Hide the comment form
     */
    hideCommentForm: function(formContainer) {
        if (!formContainer) return;
        
        // Get the parent container
        const controlsContainer = formContainer.closest('.comment-controls');
        if (!controlsContainer) return;
        
        // Hide the form container
        formContainer.classList.add('d-none');
        
        // Show the "Add a comment" link again
        const link = controlsContainer.querySelector('.add-comment-link');
        if (link) link.classList.remove('d-none');
        
        // Clear the textarea
        const textarea = formContainer.querySelector('.comment-text');
        if (textarea) {
            textarea.value = '';
            textarea.style.height = '36px'; // Reset height
        }
    },
    
    /**
     * Handle the comment form submission
     */
    handleCommentSubmit: function(submitButton) {
        const form = submitButton.closest('form');
        if (!form) return;
        
        // Get the comment data
        const textarea = form.querySelector('.comment-text');
        const targetType = form.querySelector('[name="targetType"]').value;
        const targetId = form.querySelector('[name="targetId"]').value;
        
        if (!textarea || !targetType || !targetId) return;
        
        const commentText = textarea.value.trim();
        if (commentText === '') {
            textarea.focus();
            return;
        }
        
        // Disable the button while submitting
        submitButton.disabled = true;
        submitButton.innerHTML = '<i class="bi bi-hourglass-split"></i>';
        
        // Submit the comment using the SignalR client
        DevCommunitySignalR.submitComment(commentText, targetType, targetId, (success, result) => {
            // Re-enable the button
            submitButton.disabled = false;
            submitButton.innerHTML = '<i class="bi bi-send-fill"></i>';
            
            if (success) {
                // Hide the form - will be replaced by the real-time comment
                this.hideCommentForm(form.closest('.comment-form-container'));
                
                // If not using real-time, manually add the comment
                if (!window.DevCommunitySignalR.isConnected) {
                    // Get container to append comment
                    let container;
                    if (targetType === 'Question') {
                        container = document.querySelector('.question-comments-container');
                    } else if (targetType === 'Answer') {
                        container = document.querySelector(`.answer-card [name="answerId"][value="${targetId}"]`)
                            ?.closest('.answer-card')?.querySelector('.answer-comments-container');
                    }
                    
                    if (container) {
                        // Remove "no comments" message if present
                        const noCommentsMsg = container.querySelector('.no-comments-message');
                        if (noCommentsMsg) noCommentsMsg.remove();
                        
                        // Get current user details
                        const username = document.querySelector('.navbar .nav-item.dropdown .dropdown-toggle')?.textContent.trim() || 'You';
                        const avatar = document.querySelector('.navbar .nav-item.dropdown img')?.src || '/images/default-avatar.png';
                        
                        // Create comment HTML
                        const date = new Date();
                        const dateStr = date.toLocaleDateString('en-US', { 
                            year: 'numeric', month: 'short', day: 'numeric', 
                            hour: '2-digit', minute: '2-digit'
                        });
                        
                        const commentHtml = `
                            <div class="comment-item highlight-new" data-comment-id="${result.commentId || 'new'}">
                                <div class="card-body py-2">
                                    <div class="d-flex">
                                        <div class="comment-avatar me-2">
                                            <img src="${avatar}" alt="${username}" class="rounded-circle">
                                        </div>
                                        <div class="comment-content flex-grow-1">
                                            <div class="comment-text">${commentText}</div>
                                            <div class="comment-meta small text-muted">
                                                <span class="comment-author">${username}</span>
                                                <span class="mx-1">•</span>
                                                <span class="comment-date" title="${dateStr}">Vừa xong</span>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        `;
                        
                        // Append to container
                        container.insertAdjacentHTML('beforeend', commentHtml);
                        
                        // Update comment count
                        const countBadge = container.closest('.comment-section').querySelector('.comment-count');
                        if (countBadge) {
                            const currentCount = parseInt(countBadge.textContent || '0');
                            countBadge.textContent = currentCount + 1;
                        }
                    }
                }
            } else {
                // Show error message
                const errorDiv = document.createElement('div');
                errorDiv.className = 'text-danger small mt-1';
                errorDiv.textContent = result || 'Error posting comment';
                form.appendChild(errorDiv);
                
                setTimeout(() => {
                    if (errorDiv.parentNode) {
                        errorDiv.parentNode.removeChild(errorDiv);
                    }
                }, 3000);
            }
        });
    }
};

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    CommentUI.init();
}); 