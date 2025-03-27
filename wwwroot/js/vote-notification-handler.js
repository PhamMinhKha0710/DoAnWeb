/**
 * Vote Notification Handler
 * Handles displaying vote notifications in real-time
 */

document.addEventListener('DOMContentLoaded', function() {
    // Initialize notification handling if the user is logged in
    initVoteNotifications();
});

/**
 * Initialize vote notification handling
 */
function initVoteNotifications() {
    // Check if the notification connection is available from signalR-client.js
    if (typeof notificationConnection !== 'undefined') {
        // The connection might already be established in signalR-client.js
        setupVoteNotificationListener(notificationConnection);
    } else if (window.notificationConnection) {
        // Alternative reference if global variable exists
        setupVoteNotificationListener(window.notificationConnection);
    } else if (typeof DevCommunitySignalR !== 'undefined' && DevCommunitySignalR.notificationConnection) {
        // Use the connection from DevCommunitySignalR
        setupVoteNotificationListener(DevCommunitySignalR.notificationConnection);
    } else {
        // If SignalR is loaded but no connection exists yet, wait and retry
        if (typeof signalR !== 'undefined') {
            console.log('Notification connection not found, will initialize vote notification handler when available');
            
            // Check periodically for the notification connection
            const checkInterval = setInterval(function() {
                if (window.notificationConnection || 
                    (typeof DevCommunitySignalR !== 'undefined' && DevCommunitySignalR.notificationConnection)) {
                    
                    clearInterval(checkInterval);
                    const conn = window.notificationConnection || DevCommunitySignalR.notificationConnection;
                    setupVoteNotificationListener(conn);
                }
            }, 1000);
        } else {
            console.warn('SignalR not loaded, vote notifications will not work');
        }
    }
}

/**
 * Set up the listener for vote notifications
 */
function setupVoteNotificationListener(connection) {
    if (!connection) return;
    
    // Make sure we don't add multiple listeners for the same event
    connection.off("ReceiveNotification");
    
    // Set up listener for vote notifications
    connection.on("ReceiveNotification", function(notification) {
        console.log("Received notification:", notification);
        
        // Check if this is a vote notification
        if (notification && (notification.type === "Vote" || notification.type === "vote")) {
            // Show a specially styled notification for votes
            showVoteNotification(notification);
            
            // Update UI if we're on the relevant page
            updateVoteCounters(notification);
            
            // Add to notification dropdown using NotificationHandler if available
            addVoteToNotificationDropdown(notification);
        }
    });
}

/**
 * Show a specially styled notification for votes
 */
function showVoteNotification(notification) {
    // Get or create the toast container
    let toastContainer = document.getElementById('toast-container');
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.id = 'toast-container';
        toastContainer.className = 'position-fixed top-0 end-0 p-3';
        toastContainer.style.zIndex = '1080';
        document.body.appendChild(toastContainer);
    }
    
    // Create a unique ID for this toast
    const toastId = 'toast-' + Date.now();
    
    // Create the toast element
    const toast = document.createElement('div');
    toast.id = toastId;
    toast.className = 'toast vote-toast showing';
    toast.setAttribute('role', 'alert');
    toast.setAttribute('aria-live', 'assertive');
    toast.setAttribute('aria-atomic', 'true');
    
    // Toast header with vote icon
    const header = document.createElement('div');
    header.className = 'vote-toast-header d-flex align-items-center';
    
    const icon = document.createElement('i');
    icon.className = 'bi bi-hand-thumbs-up-fill me-2 text-primary';
    icon.style.fontSize = '1.1rem';
    
    const title = document.createElement('strong');
    title.className = 'vote-toast-title me-auto';
    title.textContent = notification.title || 'New Upvote';
    
    const closeButton = document.createElement('button');
    closeButton.type = 'button';
    closeButton.className = 'btn-close btn-sm';
    closeButton.setAttribute('aria-label', 'Close');
    closeButton.onclick = function() {
        const toast = document.getElementById(toastId);
        if (toast) {
            toast.classList.remove('show');
            setTimeout(() => toast.remove(), 300);
        }
    };
    
    header.appendChild(icon);
    header.appendChild(title);
    header.appendChild(closeButton);
    
    // Toast body with message
    const body = document.createElement('div');
    body.className = 'vote-toast-body';
    body.textContent = notification.message;
    
    // Action buttons if we have a questionId
    let actionContainer = null;
    if (notification.questionId) {
        actionContainer = document.createElement('div');
        actionContainer.className = 'vote-toast-actions mt-2 d-flex';
        
        const viewButton = document.createElement('a');
        viewButton.href = `/Questions/Details/${notification.questionId}`;
        if (notification.answerId) {
            viewButton.href += `#answer-${notification.answerId}`;
        }
        viewButton.className = 'btn btn-sm btn-primary';
        viewButton.textContent = 'View';
        
        actionContainer.appendChild(viewButton);
    }
    
    // Assemble toast
    toast.appendChild(header);
    toast.appendChild(body);
    if (actionContainer) {
        toast.appendChild(actionContainer);
    }
    
    // Add to container
    toastContainer.appendChild(toast);
    
    // Show the toast
    setTimeout(() => {
        toast.classList.remove('showing');
        toast.classList.add('show');
    }, 10);
    
    // Auto hide after 6 seconds
    setTimeout(() => {
        const toastElement = document.getElementById(toastId);
        if (toastElement) {
            toastElement.classList.remove('show');
            setTimeout(() => toastElement.remove(), 300);
        }
    }, 6000);
}

/**
 * Add the vote notification to the notification dropdown
 */
function addVoteToNotificationDropdown(notification) {
    // Check if NotificationHandler is available
    if (typeof NotificationHandler !== 'undefined' && NotificationHandler.addNotificationToDropdown) {
        // Use NotificationHandler to add to dropdown
        NotificationHandler.addNotificationToDropdown(notification);
    } else {
        // Manual implementation to add to notification dropdown
        const dropdownList = document.querySelector('.notification-list');
        if (!dropdownList) return;
        
        // Clean up no notifications message if present
        const emptyState = dropdownList.querySelector('.text-center.py-4');
        if (emptyState) {
            dropdownList.innerHTML = '';
        }
        
        // Create notification item for dropdown
        const notificationItem = document.createElement('a');
        
        // Set URL based on if it's a question or answer vote
        if (notification.answerId) {
            notificationItem.href = `/Questions/Details/${notification.questionId}#answer-${notification.answerId}`;
        } else {
            notificationItem.href = `/Questions/Details/${notification.questionId}`;
        }
        
        notificationItem.className = 'notification-item unread text-decoration-none text-reset';
        
        // Build notification content
        notificationItem.innerHTML = `
            <div class="d-flex align-items-start">
                <div class="notification-icon">
                    <i class="bi bi-arrow-up-circle text-danger"></i>
                </div>
                <div class="flex-grow-1">
                    <div class="notification-title">${notification.title || 'New Upvote'}</div>
                    <p class="mb-0 small">${notification.message}</p>
                    <small class="notification-time">just now</small>
                </div>
            </div>
        `;
        
        // Add to top of dropdown list
        if (dropdownList.firstChild) {
            dropdownList.insertBefore(notificationItem, dropdownList.firstChild);
        } else {
            dropdownList.appendChild(notificationItem);
        }
        
        // Update notification badge count
        const badge = document.querySelector('.notification-badge');
        if (badge) {
            let count = parseInt(badge.textContent || '0');
            badge.textContent = count + 1;
            
            // Make sure badge is visible
            badge.style.display = 'inline-block';
        } else {
            // Create badge if it doesn't exist
            const bellIcon = document.querySelector('.bi-bell');
            if (bellIcon) {
                const parentLink = bellIcon.closest('a');
                if (parentLink) {
                    const newBadge = document.createElement('span');
                    newBadge.className = 'badge rounded-pill bg-danger position-absolute top-0 start-100 translate-middle notification-badge';
                    newBadge.textContent = '1';
                    parentLink.appendChild(newBadge);
                }
            }
        }
    }
}

/**
 * Update vote counters on the page if we're viewing the relevant question
 */
function updateVoteCounters(notification) {
    try {
        // Check if we're on a question page and it's the same question
        const questionMeta = document.querySelector('meta[name="question-id"]');
        if (!questionMeta) return;
        
        const currentQuestionId = parseInt(questionMeta.getAttribute('content'));
        if (isNaN(currentQuestionId) || currentQuestionId !== notification.questionId) return;
        
        // If this is for a specific answer
        if (notification.answerId) {
            // Find the answer score element
            const answerScores = document.querySelectorAll(`.vote-count[data-id="${notification.answerId}"][data-type="answer"]`);
            answerScores.forEach(scoreElement => {
                scoreElement.textContent = notification.score;
                
                // Add a highlight animation
                scoreElement.classList.add('score-updated');
                setTimeout(() => scoreElement.classList.remove('score-updated'), 2000);
            });
        } else {
            // Find the question score element
            const questionScores = document.querySelectorAll(`.vote-count[data-id="${notification.questionId}"][data-type="question"]`);
            questionScores.forEach(scoreElement => {
                scoreElement.textContent = notification.score;
                
                // Add a highlight animation
                scoreElement.classList.add('score-updated');
                setTimeout(() => scoreElement.classList.remove('score-updated'), 2000);
            });
        }
    } catch (error) {
        console.error('Error updating vote counters:', error);
    }
} 