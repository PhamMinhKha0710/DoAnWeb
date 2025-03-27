/**
 * Vote Handler JavaScript
 * Handles AJAX voting and UI updates
 */

// Main vote function called by vote buttons
function vote(itemId, itemType, voteType) {
    // Construct the payload
    const data = {
        itemId: itemId,
        itemType: itemType,
        voteType: voteType
    };
    
    // Get the CSRF token from the hidden form
    const csrfForm = document.getElementById('csrf-form');
    const token = csrfForm ? csrfForm.querySelector('input[name="__RequestVerificationToken"]')?.value : null;
    
    // Configure the fetch request
    const options = {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(data)
    };
    
    // Add CSRF token if available
    if (token) {
        options.headers['RequestVerificationToken'] = token;
    }
    
    // Send the request
    fetch('/Vote/Cast', options)
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            if (data.success) {
                // Update the UI
                updateVoteUI(itemId, itemType, data.newScore, data.userVote);
                
                // Show success toast
                let message = '';
                if (data.userVote === 0) {
                    message = 'Vote removed successfully';
                } else if (data.userVote === 1) {
                    message = `Upvoted ${itemType} successfully`;
                } else {
                    message = `Downvoted ${itemType} successfully`;
                }
                
                showToast('Success', message, 'success');
            } else {
                // Show error toast
                showToast('Error', data.message || 'Failed to process vote', 'error');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            showToast('Error', 'An error occurred while processing your vote', 'error');
        });
}

// Update UI after voting
function updateVoteUI(itemId, itemType, newScore, userVote) {
    // Update score display
    const scoreElements = document.querySelectorAll(`.vote-count[data-id="${itemId}"][data-type="${itemType}"]`);
    scoreElements.forEach(element => {
        element.textContent = newScore;
    });
    
    // Update vote buttons
    const upButtons = document.querySelectorAll(`.vote-btn-up[data-id="${itemId}"][data-type="${itemType}"]`);
    const downButtons = document.querySelectorAll(`.vote-btn-down[data-id="${itemId}"][data-type="${itemType}"]`);
    
    upButtons.forEach(button => {
        const icon = button.querySelector('i');
        const text = button.querySelector('span');
        
        if (userVote === 1) {
            // User upvoted
            button.classList.add('active', 'btn-primary');
            button.classList.remove('btn-outline-success');
            icon.classList.remove('bi-arrow-up');
            icon.classList.add('bi-arrow-up-circle-fill');
            if (text) text.textContent = 'Upvoted';
        } else {
            // User didn't upvote
            button.classList.remove('active', 'btn-primary');
            button.classList.add('btn-outline-success');
            icon.classList.add('bi-arrow-up');
            icon.classList.remove('bi-arrow-up-circle-fill');
            if (text) text.textContent = 'Upvote';
        }
    });
    
    downButtons.forEach(button => {
        const icon = button.querySelector('i');
        const text = button.querySelector('span');
        
        if (userVote === -1) {
            // User downvoted
            button.classList.add('active-downvote', 'btn-danger');
            button.classList.remove('btn-outline-danger');
            icon.classList.remove('bi-arrow-down');
            icon.classList.add('bi-arrow-down-circle-fill');
            if (text) text.textContent = 'Downvoted';
        } else {
            // User didn't downvote
            button.classList.remove('active-downvote', 'btn-danger');
            button.classList.add('btn-outline-danger');
            icon.classList.add('bi-arrow-down');
            icon.classList.remove('bi-arrow-down-circle-fill');
            if (text) text.textContent = 'Downvote';
        }
    });
}

// Show toast notification
function showToast(title, message, type = 'info') {
    // Create toast container if it doesn't exist
    let container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        container.className = 'position-fixed top-0 end-0 p-3';
        container.style.zIndex = '1080';
        document.body.appendChild(container);
    }
    
    // Create toast element
    const toastId = 'toast-' + Date.now();
    const toastElement = document.createElement('div');
    toastElement.id = toastId;
    toastElement.className = `toast toast-${type} showing`;
    toastElement.role = 'alert';
    toastElement.setAttribute('aria-live', 'assertive');
    toastElement.setAttribute('aria-atomic', 'true');
    
    // Create toast header
    const header = document.createElement('div');
    header.className = 'toast-header';
    
    const titleElement = document.createElement('strong');
    titleElement.className = 'toast-title me-auto';
    titleElement.textContent = title;
    
    const closeButton = document.createElement('button');
    closeButton.type = 'button';
    closeButton.className = 'toast-close';
    closeButton.setAttribute('data-bs-dismiss', 'toast');
    closeButton.setAttribute('aria-label', 'Close');
    closeButton.innerHTML = '&times;';
    closeButton.onclick = function() {
        hideToast(toastId);
    };
    
    header.appendChild(titleElement);
    header.appendChild(closeButton);
    
    // Create toast body
    const body = document.createElement('div');
    body.className = 'toast-body';
    body.textContent = message;
    
    // Add elements to toast
    toastElement.appendChild(header);
    toastElement.appendChild(body);
    
    // Add toast to container
    container.appendChild(toastElement);
    
    // Show toast
    setTimeout(() => {
        toastElement.classList.remove('showing');
        toastElement.classList.add('show');
    }, 50);
    
    // Hide toast after a delay
    setTimeout(() => {
        hideToast(toastId);
    }, 5000);
    
    return toastId;
}

// Hide toast notification
function hideToast(toastId) {
    const toast = document.getElementById(toastId);
    if (toast) {
        toast.classList.remove('show');
        toast.classList.add('hiding');
        
        // Remove after animation
        setTimeout(() => {
            toast.remove();
        }, 300);
    }
}

// Update vote UI when receiving real-time notifications
document.addEventListener('DOMContentLoaded', function() {
    // Check if the notification connection exists
    if (window.notificationConnection) {
        // Listen for vote notifications
        notificationConnection.on('ReceiveVoteUpdate', function(data) {
            if (data && data.itemId && data.itemType && data.newScore !== undefined) {
                // Update score for all elements matching this item
                updateVoteUI(data.itemId, data.itemType, data.newScore, data.userVote);
            }
        });
    }
}); 