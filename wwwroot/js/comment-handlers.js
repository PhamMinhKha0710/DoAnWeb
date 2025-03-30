// comment-handlers.js - Handles comment and reply UI interactions

document.addEventListener('DOMContentLoaded', function() {
    // Initialize event handlers
    initCommentHandlers();
    
    // Initialize timeago for relative timestamps
    if (typeof timeago !== 'undefined') {
        timeago.render(document.querySelectorAll('.comment-date'));
    }
});

/**
 * Initialize all comment-related event handlers
 */
function initCommentHandlers() {
    // Initialize form submission handlers
    initCommentFormHandlers();
    
    // Initialize reply link handlers
    initReplyLinkHandlers();
    
    // Initialize toggle reply visibility handlers
    initToggleReplyHandlers();
    
    // Periodically refresh relative timestamps
    setInterval(refreshTimestamps, 60000); // Update every minute
}

/**
 * Initialize comment form submission handlers
 */
function initCommentFormHandlers() {
    // Handle main comment form submissions
    document.querySelectorAll('.comment-form').forEach(form => {
        form.addEventListener('submit', handleCommentFormSubmit);
    });
    
    // We'll attach event listeners to reply forms when they're created
}

/**
 * Initialize reply link click handlers
 */
function initReplyLinkHandlers() {
    // Use event delegation for all reply links
    document.addEventListener('click', event => {
        const replyLink = event.target.closest('.reply-link');
        if (!replyLink) return;
        
        event.preventDefault();
        
        // Get necessary data attributes
        const commentId = replyLink.closest('.comment-item').dataset.commentId;
        const parentCommentId = replyLink.dataset.parentCommentId || commentId;
        const replyToUser = replyLink.dataset.replyTo || 
                           replyLink.closest('.comment-item').querySelector('.comment-author').textContent;
        
        // Add the reply form
        addReplyFormTo(commentId, parentCommentId, replyToUser);
    });
}

/**
 * Initialize toggle reply visibility handlers
 */
function initToggleReplyHandlers() {
    // Use event delegation for toggle links
    document.addEventListener('click', event => {
        const toggleLink = event.target.closest('.view-replies-toggle');
        if (!toggleLink) return;
        
        event.preventDefault();
        
        // Toggle reply visibility
        const commentItem = toggleLink.closest('.comment-item');
        const repliesContainer = commentItem.querySelector('.comment-replies');
        
        if (!repliesContainer) return;
        
        // Toggle visibility
        const isVisible = !repliesContainer.classList.contains('d-none');
        
        if (isVisible) {
            // Hide replies
            repliesContainer.classList.add('d-none');
            toggleLink.innerHTML = `<i class="bi bi-plus-circle"></i> Xem ${repliesContainer.dataset.replyCount || repliesContainer.querySelectorAll('.comment-item').length} phản hồi`;
            toggleLink.classList.remove('text-primary');
        } else {
            // Show replies
            repliesContainer.classList.remove('d-none');
            toggleLink.innerHTML = `<i class="bi bi-dash-circle"></i> Ẩn phản hồi`;
            toggleLink.classList.add('text-primary');
        }
    });
}

/**
 * Handle comment form submission
 */
function handleCommentFormSubmit(event) {
    event.preventDefault();
    
    const form = event.target;
    const textarea = form.querySelector('textarea');
    const text = textarea.value.trim();
    
    if (!text) {
        // Highlight empty textarea
        textarea.classList.add('is-invalid');
        setTimeout(() => textarea.classList.remove('is-invalid'), 2000);
        return;
    }
    
    // Get target info from form data attributes
    const targetType = form.dataset.targetType;
    const targetId = form.dataset.targetId;
    const parentCommentId = form.dataset.parentCommentId || null;
    
    // Disable the form while submitting
    const submitBtn = form.querySelector('button[type="submit"]');
    const originalBtnText = submitBtn.innerHTML;
    submitBtn.disabled = true;
    submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Đang gửi...';
    
    // Create options object with parentCommentId if it exists
    const options = parentCommentId ? { parentCommentId: parseInt(parentCommentId) } : null;
    
    // Use SignalR client to submit
    DevCommunitySignalR.submitComment(
        text, 
        targetType, 
        parseInt(targetId), 
        options
    ).then(result => {
        // On success
        if (result) {
            // Clear the form
            textarea.value = '';
            
            // Remove the reply form if this is a reply
            if (parentCommentId) {
                form.closest('.reply-form-container').remove();
            }
        }
    }).catch(error => {
        console.error('Error submitting comment:', error);
        alert('Có lỗi xảy ra khi gửi bình luận. Vui lòng thử lại sau.');
    }).finally(() => {
        // Re-enable the form
        submitBtn.disabled = false;
        submitBtn.innerHTML = originalBtnText;
    });
}

/**
 * Add a reply form to a comment
 */
function addReplyFormTo(commentId, parentCommentId, replyToUser) {
    // Remove any existing reply forms
    document.querySelectorAll('.reply-form-container').forEach(container => {
        container.remove();
    });
    
    // Get the comment element
    const commentElement = document.querySelector(`.comment-item[data-comment-id="${commentId}"]`);
    if (!commentElement) return;
    
    // Get target information from the nearest comment section
    const commentSection = commentElement.closest('.comment-section');
    const targetType = commentSection.dataset.targetType;
    const targetId = commentSection.dataset.targetId;
    
    // Get current user avatar
    const currentUserAvatar = document.querySelector('.navbar .nav-item.dropdown .avatar-img')?.src ||
                             '/images/default-avatar.png';
    
    // Create reply form HTML
    const replyFormHtml = `
        <div class="reply-form-container mt-2 mb-3 animate__animated animate__fadeIn">
            <div class="d-flex">
                <div class="reply-avatar me-2">
                    <img src="${currentUserAvatar}" alt="Your avatar" class="rounded-circle" width="30" height="30">
                </div>
                <div class="flex-grow-1">
                    <form class="reply-form" data-target-type="${targetType}" data-target-id="${targetId}" data-parent-comment-id="${parentCommentId}">
                        <div class="form-group mb-2">
                            <textarea class="form-control form-control-sm" rows="2" 
                                     placeholder="Trả lời cho @${replyToUser}..." required></textarea>
                        </div>
                        <div class="d-flex justify-content-end">
                            <button type="button" class="btn btn-sm btn-link cancel-reply me-2">Hủy</button>
                            <button type="submit" class="btn btn-sm btn-primary">Gửi phản hồi</button>
                        </div>
                    </form>
                </div>
            </div>
        </div>
    `;
    
    // Insert the form
    if (commentElement.classList.contains('comment-reply')) {
        // If this is a reply, add the form after this reply
        commentElement.insertAdjacentHTML('afterend', replyFormHtml);
    } else {
        // If this is a parent comment, insert before the replies container or at the end
        const repliesContainer = commentElement.querySelector('.comment-replies');
        if (repliesContainer) {
            repliesContainer.insertAdjacentHTML('beforebegin', replyFormHtml);
        } else {
            commentElement.insertAdjacentHTML('beforeend', replyFormHtml);
        }
    }
    
    // Focus the textarea
    const textarea = commentElement.parentNode.querySelector('.reply-form textarea');
    if (textarea) textarea.focus();
    
    // Setup event handlers for the new form
    const replyForm = commentElement.parentNode.querySelector('.reply-form');
    if (replyForm) {
        replyForm.addEventListener('submit', handleCommentFormSubmit);
        
        // Cancel button handler
        replyForm.querySelector('.cancel-reply').addEventListener('click', () => {
            replyForm.closest('.reply-form-container').remove();
        });
    }
}

/**
 * Refresh relative timestamps
 */
function refreshTimestamps() {
    if (typeof timeago !== 'undefined') {
        timeago.render(document.querySelectorAll('.comment-date'));
    }
}

// Initialize when the document is ready
if (document.readyState === 'complete' || document.readyState === 'interactive') {
    initCommentHandlers();
} else {
    document.addEventListener('DOMContentLoaded', initCommentHandlers);
} 