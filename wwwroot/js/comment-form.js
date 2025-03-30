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
            
            // Handle "Reply" link clicks
            if (event.target.matches('.reply-link') || event.target.closest('.reply-link')) {
                event.preventDefault();
                const link = event.target.matches('.reply-link') ? event.target : event.target.closest('.reply-link');
                this.showReplyForm(link);
            }
            
            // Handle "View replies" toggle
            if (event.target.matches('.view-replies-toggle') || event.target.closest('.view-replies-toggle')) {
                event.preventDefault();
                const toggle = event.target.matches('.view-replies-toggle') ? event.target : event.target.closest('.view-replies-toggle');
                this.toggleReplies(toggle);
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
     * Show reply form when "Reply" is clicked
     */
    showReplyForm: function(replyLink) {
        // Get the comment element
        const commentElement = replyLink.closest('.comment-item');
        if (!commentElement) return;
        
        // Get comment ID - either from the closest comment-item or from data attribute
        const commentId = replyLink.dataset.parentCommentId || commentElement.dataset.commentId;
        if (!commentId) return;
        
        // Get the name of the user being replied to
        const replyToUser = replyLink.dataset.replyTo || commentElement.querySelector('.comment-author')?.textContent || 'this user';
        
        // Add reply form if it doesn't exist
        this.addReplyFormTo(commentElement, commentId, replyToUser);
        
        // Show the form
        const replyForm = commentElement.querySelector('.reply-form-container');
        if (replyForm) {
            replyForm.classList.remove('d-none');
            
            // Focus the textarea
            const textarea = replyForm.querySelector('.comment-text');
            if (textarea) {
                textarea.focus();
            }
        }
    },
    
    /**
     * Hide reply form
     */
    hideReplyForm: function(commentElement) {
        const replyForm = commentElement.querySelector('.reply-form-container');
        if (replyForm) {
            replyForm.classList.add('d-none');
            
            // Clear the textarea
            const textarea = replyForm.querySelector('.comment-text');
            if (textarea) {
                textarea.value = '';
            }
        }
    },
    
    /**
     * Toggle showing/hiding replies
     */
    toggleReplies: function(toggle) {
        const commentElement = toggle.closest('.comment-item');
        if (!commentElement) return;
        
        const repliesContainer = commentElement.querySelector('.comment-replies');
        if (!repliesContainer) return;
        
        // Toggle visibility
        if (repliesContainer.classList.contains('d-none')) {
            repliesContainer.classList.remove('d-none');
            toggle.innerHTML = `<i class="bi bi-dash-circle"></i> Hide replies`;
            toggle.classList.add('text-primary');
        } else {
            repliesContainer.classList.add('d-none');
            toggle.innerHTML = `<i class="bi bi-plus-circle"></i> View ${repliesContainer.dataset.replyCount || ''} replies`;
            toggle.classList.remove('text-primary');
        }
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
     * Render a comment with replies structure
     */
    renderCommentWithReplies: function(comment, parentContainer) {
        // Create base comment HTML
        const commentHtml = this.createCommentHtml(comment);
        
        // Add to container
        parentContainer.insertAdjacentHTML('beforeend', commentHtml);
        
        // If there are replies, create a replies container
        if (comment.replies && comment.replies.length > 0) {
            const commentElement = parentContainer.querySelector(`.comment-item[data-comment-id="${comment.commentId}"]`);
            
            // Add replies container
            const repliesHtml = `
                <div class="comment-replies ms-4 mt-2">
                    ${comment.replies.map(reply => this.createCommentHtml(reply, true)).join('')}
                </div>
            `;
            
            commentElement.insertAdjacentHTML('beforeend', repliesHtml);
        }
    },
    
    /**
     * Create HTML for a single comment or reply
     */
    createCommentHtml: function(comment, isReply = false) {
        const date = new Date(comment.createdDate);
        const dateStr = date.toLocaleDateString('en-US', { 
            year: 'numeric', month: 'short', day: 'numeric', 
            hour: '2-digit', minute: '2-digit'
        });
        
        const hasReplies = comment.replies && comment.replies.length > 0;
        const replyCountBadge = hasReplies ? 
            `<a href="#" class="view-replies-toggle text-decoration-none small">
                <i class="bi bi-plus-circle"></i> View ${comment.replies.length} replies
            </a>` : '';
            
        const replyLink = !isReply ? 
            `<a href="#" class="reply-link text-decoration-none small text-muted ms-2">
                <i class="bi bi-reply"></i> Reply
            </a>` : '';
        
        return `
            <div class="comment-item py-2 ${isReply ? 'comment-reply' : ''}" data-comment-id="${comment.commentId}">
                <div class="d-flex">
                    <div class="comment-avatar me-2">
                        <img src="${comment.userAvatar || '/images/default-avatar.png'}" 
                             alt="${comment.userDisplayName || comment.userName}" class="rounded-circle" width="36" height="36">
                    </div>
                    <div class="flex-grow-1">
                        <div class="comment-content">
                            <div class="comment-text">${comment.body}</div>
                        </div>
                        <div class="comment-meta">
                            <span class="comment-author fw-medium">${comment.userDisplayName || comment.userName}</span>
                            <span class="mx-1">•</span>
                            <span class="comment-date text-secondary" title="${dateStr}">
                                ${this.getRelativeTime(date)}
                            </span>
                            ${replyLink}
                        </div>
                        ${replyCountBadge}
                    </div>
                </div>
            </div>
        `;
    },
    
    /**
     * Get relative time string (e.g. "2 hours ago")
     */
    getRelativeTime: function(date) {
        const now = new Date();
        const diffMs = now - date;
        const diffSeconds = Math.floor(diffMs / 1000);
        const diffMinutes = Math.floor(diffSeconds / 60);
        const diffHours = Math.floor(diffMinutes / 60);
        const diffDays = Math.floor(diffHours / 24);
        
        if (diffSeconds < 60) return 'Just now';
        if (diffMinutes < 60) return `${diffMinutes}m ago`;
        if (diffHours < 24) return `${diffHours}h ago`;
        if (diffDays < 7) return `${diffDays}d ago`;
        
        return date.toLocaleDateString('en-US', { 
            year: 'numeric', month: 'short', day: 'numeric'
        });
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
            submitButton.innerHTML = targetType === 'Comment' ? 
                '<i class="bi bi-send-fill me-1"></i> Reply' : 
                '<i class="bi bi-send-fill"></i>';
            
            if (success) {
                // If this is a reply, hide the reply form
                if (targetType === 'Comment') {
                    const commentElement = form.closest('.comment-item');
                    if (commentElement) {
                        this.hideReplyForm(commentElement);
                    }
                } else {
                    // Hide the regular comment form
                    this.hideCommentForm(form.closest('.comment-form-container'));
                }
                
                // If not using real-time, manually add the comment
                if (!window.DevCommunitySignalR.isConnected) {
                    // TODO: Handle manual comment/reply rendering if SignalR is not connected
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
    },
    
    /**
     * Add a reply form to a comment
     */
    addReplyFormTo: function(commentElement, commentId, replyToUser) {
        if (!commentElement) return;
        
        // Remove any existing reply form
        const existingForm = commentElement.querySelector('.reply-form-container');
        if (existingForm) {
            existingForm.remove();
        }
        
        // Get current user avatar
        let currentUserAvatar = document.querySelector('.navbar .nav-item.dropdown img')?.src || '/images/default-avatar.png';
        
        // Create reply form HTML
        const replyFormHtml = `
            <div class="reply-form-container mt-2 animate__animated animate__fadeIn">
                <div class="d-flex align-items-start">
                    <img src="${currentUserAvatar}" class="current-user-avatar" alt="Your avatar">
                    <form class="reply-form flex-grow-1">
                        <div class="replying-to text-muted small mb-1">
                            <i class="bi bi-reply"></i> Trả lời <span class="fw-medium">@${replyToUser}</span>
                        </div>
                        <div class="form-group">
                            <textarea class="form-control comment-text" 
                                  placeholder="Viết trả lời cho ${replyToUser}..."></textarea>
                        </div>
                        <div class="d-flex justify-content-end mt-2">
                            <button type="button" class="btn btn-sm btn-outline-secondary me-2 cancel-reply-btn">
                                Hủy
                            </button>
                            <button type="submit" class="btn btn-sm btn-primary comment-submit-btn">
                                <i class="bi bi-send-fill me-1"></i> Gửi
                            </button>
                        </div>
                        <input type="hidden" name="targetType" value="Comment">
                        <input type="hidden" name="targetId" value="${commentId}">
                    </form>
                </div>
            </div>
        `;
        
        // Add to comment element
        commentElement.querySelector('.flex-grow-1').insertAdjacentHTML('beforeend', replyFormHtml);
        
        // Add cancel button handler
        const cancelBtn = commentElement.querySelector('.cancel-reply-btn');
        if (cancelBtn) {
            cancelBtn.addEventListener('click', () => {
                this.hideReplyForm(commentElement);
            });
        }
    }
};

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    CommentUI.init();
}); 