/**
 * Notification Handler - Manages real-time notifications using SignalR
 * Provides toast notifications and notification badge updates
 */
"use strict";

// Notification handler module
const NotificationHandler = (function () {
    // Private variables
    let connection = null;
    let notificationCount = 0;
    let isConnected = false;
    let retryCount = 0;
    const maxRetries = 5;
    
    // Initialize the notification system
    function init() {
        console.log("NotificationHandler: Initializing...");
        
        // Only initialize if user is logged in (check if the notification badge exists)
        if (document.getElementById('notification-badge')) {
            loadUnreadCount();
            initializeSignalR();
            initializeToastContainer();
        } else {
            console.log("Notification badge not found, user may not be logged in");
        }
    }
    
    // Create toast container if it doesn't exist
    function initializeToastContainer() {
        if (!document.querySelector('.toast-container')) {
            const toastContainer = document.createElement('div');
            toastContainer.className = 'toast-container position-fixed bottom-0 end-0 p-3';
            toastContainer.style.zIndex = '1050';
            document.body.appendChild(toastContainer);
        }
    }
    
    // Load unread notification count
    function loadUnreadCount() {
        console.log("NotificationHandler: Loading unread count...");
        
        fetch('/Notifications/UnreadCount')
            .then(response => {
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                return response.json();
            })
            .then(data => {
                console.log("Unread notifications count:", data.count);
                updateNotificationBadge(data.count);
            })
            .catch(error => {
                console.error('Error fetching notification count:', error);
            });
    }
    
    // Initialize SignalR connection
    function initializeSignalR() {
        try {
            console.log("NotificationHandler: Initializing SignalR...");
            
            // Check if SignalR is available
            if (typeof signalR === 'undefined') {
                console.error("SignalR library not loaded! Check if signalr.min.js is properly included.");
                setTimeout(initializeSignalR, 2000); // Retry after 2 seconds
                return;
            }
            
            // Check if user is logged in before attempting to connect to notification hub
            const isLoggedIn = document.querySelector('.nav-item.dropdown:has(.dropdown-item[href="javascript:void(0);"][onclick*="logoutForm"])') !== null || 
                            document.querySelector('form#logoutForm') !== null;
            
            if (!isLoggedIn) {
                console.log("User not logged in. NotificationHub requires authentication.");
                return; // Don't attempt to connect if not logged in
            }
            
            // Create SignalR connection with authentication
            connection = new signalR.HubConnectionBuilder()
                .withUrl('/notificationHub', {
                    // Pass authentication tokens if available
                    accessTokenFactory: () => {
                        return getAuthToken();
                    }
                })
                .withAutomaticReconnect([0, 2000, 5000, 10000, 30000]) // Retry policy
                .configureLogging(signalR.LogLevel.Information)
                .build();
            
            // Set up connection event handlers
            connection.onreconnecting(error => {
                console.log('Reconnecting to notification hub...', error);
                isConnected = false;
                showConnectionStatus('connecting');
            });
            
            connection.onreconnected(connectionId => {
                console.log('Reconnected to notification hub:', connectionId);
                isConnected = true;
                retryCount = 0;
                showConnectionStatus('connected');
                
                // Reload notification count after reconnection
                loadUnreadCount();
            });
            
            connection.onclose(error => {
                console.log('Connection closed:', error);
                isConnected = false;
                showConnectionStatus('disconnected');
                
                // Try to reconnect if max retries not reached
                if (retryCount < maxRetries) {
                    retryCount++;
                    setTimeout(startConnection, 5000);
                }
            });
            
            // Set up notification handler
            connection.on('ReceiveNotification', handleNewNotification);
            connection.on('NotificationMarkedAsRead', handleNotificationRead);
            connection.on('AllNotificationsMarkedAsRead', handleAllNotificationsRead);
            
            // Start the connection
            startConnection();
        } catch (error) {
            console.error('Error initializing SignalR:', error);
            
            // Try to recover by reinitializing after a delay
            setTimeout(initializeSignalR, 5000);
        }
    }
    
    // Show connection status indicator
    function showConnectionStatus(status) {
        let statusIndicator = document.getElementById('signalr-status-indicator');
        
        if (!statusIndicator) {
            statusIndicator = document.createElement('div');
            statusIndicator.id = 'signalr-status-indicator';
            statusIndicator.className = 'connection-status';
            document.body.appendChild(statusIndicator);
        }
        
        // Update status
        statusIndicator.className = `connection-status ${status}`;
        
        // Set text and icon based on status
        let statusText = '';
        let statusIcon = '';
        
        switch (status) {
            case 'connected':
                statusText = 'Connected';
                statusIcon = '<i class="bi bi-wifi"></i>';
                
                // Fade out connected status after 3 seconds
                setTimeout(() => {
                    statusIndicator.remove();
                }, 3000);
                break;
                
            case 'connecting':
                statusText = 'Connecting...';
                statusIcon = '<i class="bi bi-wifi-off"></i>';
                break;
                
            case 'disconnected':
                statusText = 'Disconnected';
                statusIcon = '<i class="bi bi-wifi-off"></i>';
                break;
        }
        
        statusIndicator.innerHTML = `${statusIcon} ${statusText}`;
    }
    
    // Start SignalR connection
    function startConnection() {
        if (connection) {
            console.log("NotificationHandler: Starting connection...");
            showConnectionStatus('connecting');
            
            connection.start()
                .then(() => {
                    console.log('Connected to notification hub');
                    isConnected = true;
                    retryCount = 0;
                    showConnectionStatus('connected');
                })
                .catch(error => {
                    console.error('Error connecting to notification hub:', error);
                    isConnected = false;
                    showConnectionStatus('disconnected');
                    
                    // Try to reconnect if max retries not reached
                    if (retryCount < maxRetries) {
                        retryCount++;
                        console.log(`Connection attempt ${retryCount} failed. Retrying in 5 seconds...`);
                        setTimeout(startConnection, 5000);
                    } else {
                        console.error(`Failed to connect after ${maxRetries} attempts`);
                    }
                });
        }
    }
    
    // Handle new notification
    function handleNewNotification(notification) {
        console.log("New notification received:", notification);
        
        // Increment notification count
        notificationCount++;
        updateNotificationBadge(notificationCount);
        
        // Show toast notification
        showToast(notification);
    }
    
    // Handle notification marked as read
    function handleNotificationRead(notificationId) {
        console.log("Notification marked as read:", notificationId);
        
        if (notificationCount > 0) {
            notificationCount--;
            updateNotificationBadge(notificationCount);
        }
    }
    
    // Handle all notifications marked as read
    function handleAllNotificationsRead() {
        console.log("All notifications marked as read");
        
        notificationCount = 0;
        updateNotificationBadge(0);
    }
    
    // Show toast notification
    function showToast(notification) {
        // Get toast container
        const container = document.querySelector('.toast-container');
        if (!container) {
            console.error("Toast container not found");
            return;
        }
        
        // Create toast element with a unique id
        const toastId = `toast-${Date.now()}`;
        const toastHtml = `
            <div class="toast" role="alert" aria-live="assertive" aria-atomic="true" id="${toastId}">
                <div class="toast-header">
                    <div class="notification-icon me-2">
                        ${getNotificationIcon(notification.type)}
                    </div>
                    <strong class="me-auto">${notification.title}</strong>
                    <small>${formatTime(notification.createdDate)}</small>
                    <button type="button" class="btn-close" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
                <div class="toast-body">
                    ${notification.message}
                    ${notification.url ? `<div class="mt-2"><a href="${notification.url}" class="btn btn-sm btn-primary">View</a></div>` : ''}
                </div>
            </div>
        `;
        
        // Add toast to container
        container.insertAdjacentHTML('beforeend', toastHtml);
        
        // Initialize toast and show it
        const toastElement = document.getElementById(toastId);
        if (toastElement && typeof bootstrap !== 'undefined') {
            const toast = new bootstrap.Toast(toastElement, {
                autohide: true,
                delay: 8000
            });
            toast.show();
            
            // Remove toast from DOM after hiding
            toastElement.addEventListener('hidden.bs.toast', () => {
                toastElement.remove();
            });
        } else {
            console.error("Toast element not found or Bootstrap not loaded");
            // Fallback if Bootstrap is not available
            setTimeout(() => {
                if (toastElement) toastElement.remove();
            }, 8000);
        }
    }
    
    // Update notification badge
    function updateNotificationBadge(count) {
        const badge = document.getElementById('notification-badge');
        if (badge) {
            notificationCount = count;
            if (count > 0) {
                badge.textContent = count > 99 ? '99+' : count.toString();
                badge.classList.remove('d-none');
            } else {
                badge.textContent = '';
                badge.classList.add('d-none');
            }
        }
    }
    
    // Get notification icon based on type
    function getNotificationIcon(type) {
        let icon = '<i class="bi bi-bell text-primary"></i>';
        
        switch (type) {
            case 'Answer':
                icon = '<i class="bi bi-chat-left-text text-primary"></i>';
                break;
            case 'Comment':
                icon = '<i class="bi bi-chat-dots text-secondary"></i>';
                break;
            case 'Vote':
                icon = '<i class="bi bi-hand-thumbs-up text-success"></i>';
                break;
            case 'Accept':
                icon = '<i class="bi bi-check-circle text-warning"></i>';
                break;
            case 'Mention':
                icon = '<i class="bi bi-at text-danger"></i>';
                break;
        }
        
        return icon;
    }
    
    // Format time for display
    function formatTime(dateString) {
        if (!dateString) return '';
        
        try {
            const date = new Date(dateString);
            const now = new Date();
            const diffMs = now - date;
            const diffMin = Math.floor(diffMs / 60000);
            
            if (diffMin < 1) return 'just now';
            if (diffMin < 60) return `${diffMin}m ago`;
            
            const diffHours = Math.floor(diffMin / 60);
            if (diffHours < 24) return `${diffHours}h ago`;
            
            return date.toLocaleDateString();
        } catch (error) {
            console.error("Error formatting time:", error);
            return 'unknown time';
        }
    }
    
    // Join a notification group
    function joinGroup(groupName) {
        if (isConnected && connection) {
            console.log(`Joining notification group: ${groupName}`);
            
            connection.invoke('JoinGroup', groupName)
                .then(() => {
                    console.log(`Successfully joined group: ${groupName}`);
                })
                .catch(error => {
                    console.error(`Error joining group ${groupName}:`, error);
                });
        } else {
            console.warn(`Cannot join group ${groupName}: Not connected`);
        }
    }
    
    // Leave a notification group
    function leaveGroup(groupName) {
        if (isConnected && connection) {
            console.log(`Leaving notification group: ${groupName}`);
            
            connection.invoke('LeaveGroup', groupName)
                .then(() => {
                    console.log(`Successfully left group: ${groupName}`);
                })
                .catch(error => {
                    console.error(`Error leaving group ${groupName}:`, error);
                });
        }
    }
    
    // Mark a notification as read
    function markAsRead(notificationId) {
        if (isConnected && connection) {
            console.log(`Marking notification as read: ${notificationId}`);
            
            connection.invoke('MarkAsRead', notificationId)
                .catch(error => {
                    console.error(`Error marking notification ${notificationId} as read:`, error);
                });
        }
    }
    
    // Mark all notifications as read
    function markAllAsRead() {
        if (isConnected && connection) {
            console.log("Marking all notifications as read");
            
            connection.invoke('MarkAllAsRead')
                .catch(error => {
                    console.error('Error marking all notifications as read:', error);
                });
        }
    }
    
    /**
     * Helper to get auth token from cookies or storage
     */
    function getAuthToken() {
        // Try to get from cookie
        const cookies = document.cookie.split(';');
        for (let i = 0; i < cookies.length; i++) {
            const cookie = cookies[i].trim();
            if (cookie.startsWith('.AspNetCore.Identity.Application=') || 
                cookie.startsWith('Authorization=') ||
                cookie.startsWith('DevCommunityAuth=')) {
                return cookie.split('=')[1];
            }
        }
        
        // Or from localStorage if your app uses that
        const token = localStorage.getItem('auth_token');
        if (token) return token;
        
        // Return null if no token found
        return null;
    }
    
    // Public API
    return {
        init: init,
        joinGroup: joinGroup,
        leaveGroup: leaveGroup,
        markAsRead: markAsRead,
        markAllAsRead: markAllAsRead
    };
})();

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    try {
        // First check if jQuery, SignalR, Bootstrap are available
        const prereqCheck = setInterval(function() {
            let ready = true;
            let missingDeps = [];
            
            if (typeof jQuery === 'undefined') {
                ready = false;
                missingDeps.push('jQuery');
            }
            
            if (typeof signalR === 'undefined') {
                ready = false;
                missingDeps.push('SignalR');
            }
            
            if (typeof bootstrap === 'undefined') {
                ready = false;
                missingDeps.push('Bootstrap');
            }
            
            if (ready) {
                clearInterval(prereqCheck);
                console.log("All dependencies loaded. Initializing NotificationHandler...");
                NotificationHandler.init();
            } else {
                console.log(`Waiting for dependencies: ${missingDeps.join(', ')}`);
            }
        }, 200);
        
        // Timeout after 10 seconds
        setTimeout(function() {
            clearInterval(prereqCheck);
            if (typeof signalR !== 'undefined') {
                console.log("Initialization timeout reached. Attempting to initialize anyway...");
                NotificationHandler.init();
            } else {
                console.error("SignalR unavailable after timeout. Notifications won't work.");
            }
        }, 10000);
    } catch (error) {
        console.error("Error in NotificationHandler initialization:", error);
    }
}); 