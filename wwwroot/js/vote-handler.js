/**
 * Vote Handler JavaScript
 * Handles AJAX toggle voting and UI updates
 */

// Main vote function called by vote buttons
function vote(itemId, itemType, voteType) {
    // Debug message to ensure function is called
    console.log('Vote function called with:', { itemId, itemType, voteType });
    
    // Get the button element that was clicked
    const button = document.querySelector(`.vote-button[data-id="${itemId}"][data-type="${itemType}"]`);
    if (!button) {
        console.error(`Vote button for ${itemType} ${itemId} not found`);
        // Try again after a short delay (sometimes DOM might not be fully ready)
        setTimeout(() => {
            const retryButton = document.querySelector(`.vote-button[data-id="${itemId}"][data-type="${itemType}"]`);
            if (retryButton) {
                console.log('Vote button found on retry');
                performVote(retryButton, itemId, itemType, voteType);
            } else {
                console.error('Vote button still not found after retry');
                showToast('Error', 'Could not find vote button. Please refresh the page.', 'error');
            }
        }, 500);
        return;
    }
    
    performVote(button, itemId, itemType, voteType);
}

// Separate the voting logic to avoid code duplication
function performVote(button, itemId, itemType, voteType) {
    // Get the current state
    const currentState = button.getAttribute('data-vote-state') || 'none';
    
    // Determine the new vote type
    // If voteType is 'none', we're removing the vote
    // If voteType is 'up' and currentState is already 'up', we're removing the vote
    const newVoteType = (voteType === 'none' || (voteType === 'up' && currentState === 'up')) ? 'none' : voteType;
    
    // Construct the payload
    const data = {
        itemId: itemId,
        itemType: itemType,
        voteType: newVoteType
    };
    
    // Get the CSRF token from the hidden form
    const csrfForm = document.getElementById('csrf-form');
    const token = csrfForm ? csrfForm.querySelector('input[name="__RequestVerificationToken"]')?.value : null;
    
    if (!token) {
        console.error('CSRF token not found. Make sure there is a csrf-form with a RequestVerificationToken input.');
        showToast('Error', 'Security token missing. Please refresh the page.', 'error');
        return;
    }
    
    // Configure the fetch request
    const options = {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        },
        body: JSON.stringify(data)
    };
    
    console.log('Sending vote request with options:', options);
    
    // Show loading indicator
    const loadingIndicator = button.querySelector('.vote-loading-indicator');
    if (loadingIndicator) {
        loadingIndicator.classList.remove('d-none');
    }
    
    // Send the request
    fetch('/Vote/Cast', options)
        .then(response => {
            if (!response.ok) {
                console.error('Network response was not ok:', response.status, response.statusText);
                throw new Error(`Network response was not ok: ${response.status} ${response.statusText}`);
            }
            return response.json();
        })
        .then(data => {
            console.log('Vote response:', data);
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
        })
        .finally(() => {
            // Hide loading indicator
            if (loadingIndicator) {
                loadingIndicator.classList.add('d-none');
            }
        });
}

// Update UI after voting
function updateVoteUI(itemId, itemType, newScore, userVote) {
    // Update score display
    const scoreElements = document.querySelectorAll(`.vote-count[data-id="${itemId}"][data-type="${itemType}"]`);
    scoreElements.forEach(element => {
        element.textContent = newScore;
    });
    
    // Update vote button
    const buttons = document.querySelectorAll(`.vote-button[data-id="${itemId}"][data-type="${itemType}"]`);
    
    buttons.forEach(button => {
        const icon = button.querySelector('i');
        const text = button.querySelector('span');
        
        // Set new state based on user vote
        let newState = 'none';
        if (userVote === 1) newState = 'up';
        else if (userVote === -1) newState = 'down';
        
        // Update data attribute
        button.setAttribute('data-vote-state', newState);
        
        // Update classes and text based on new state
        if (newState === 'up') {
            // Upvoted state
            button.className = 'btn btn-primary btn-sm vote-button';
            if (icon) {
                icon.className = 'bi bi-hand-thumbs-up-fill me-1';
            }
            if (text) {
                text.textContent = 'Voted ▲';
            }
        } else if (newState === 'down') {
            // Downvoted state
            button.className = 'btn btn-danger btn-sm vote-button';
            if (icon) {
                icon.className = 'bi bi-hand-thumbs-down-fill me-1';
            }
            if (text) {
                text.textContent = 'Voted ▼';
            }
        } else {
            // No vote state
            button.className = 'btn btn-outline-primary btn-sm vote-button';
            if (icon) {
                icon.className = 'bi bi-hand-thumbs-up me-1';
            }
            if (text) {
                text.textContent = 'Vote';
            }
        }
        
        // Update onclick handler to toggle appropriately
        button.setAttribute('onclick', `vote(${itemId}, '${itemType}', '${newState === 'none' ? 'up' : 'none'}')`);
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