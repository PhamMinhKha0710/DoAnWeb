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
        if (document.querySelector('.notification-badge')) {
            loadUnreadCount();
            initializeSignalR();
            initializeToastContainer();
            initializeNotificationDropdown(); // Initialize dropdown with existing notifications
            addSoundToggleToSettings();
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
                showConnectionStatus('connected');
            });
            
            connection.onclose(error => {
                console.log('Disconnected from notification hub:', error);
                isConnected = false;
                showConnectionStatus('disconnected');
                
                // Try to reconnect if not a deliberate close
                if (retryCount < maxRetries) {
                    startConnection();
                }
            });
            
            // Register notification handlers
            connection.on("ReceiveNotification", (notification) => {
                handleNewNotification(notification);
            });
            
            connection.on("NotificationMarkedAsRead", (notificationId) => {
                handleNotificationRead(notificationId);
            });
            
            connection.on("AllNotificationsMarkedAsRead", () => {
                handleAllNotificationsRead();
            });
            
            // Start the connection
            startConnection();
        } catch (error) {
            console.error("Error initializing SignalR:", error);
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
        
        // Also add notification to the dropdown
        addNotificationToDropdown(notification);
    }
    
    // Handle notification marked as read
    function handleNotificationRead(notificationId) {
        console.log("Notification marked as read:", notificationId);
        
        if (notificationCount > 0) {
            notificationCount--;
            updateNotificationBadge(notificationCount);
        }
        
        // Update the notification item in the dropdown
        const notificationItem = document.querySelector(`.notification-item[data-notification-id="${notificationId}"]`);
        if (notificationItem) {
            notificationItem.classList.remove('unread');
        }
    }
    
    // Handle all notifications marked as read
    function handleAllNotificationsRead() {
        console.log("All notifications marked as read");
        
        notificationCount = 0;
        updateNotificationBadge(0);
        
        // Update all notification items in the dropdown
        const notificationItems = document.querySelectorAll('.notification-item.unread');
        notificationItems.forEach(item => {
            item.classList.remove('unread');
        });
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
        
        // Determine icon and background color based on notification type
        let icon = 'info-circle';
        let bgColor = 'bg-primary';
        let headerText = 'Notification';
        
        // Customize based on notification type
        switch (notification.type) {
            case 'Vote':
                icon = 'arrow-up-circle';
                bgColor = 'bg-success';
                headerText = 'Upvote Received';
                break;
            case 'Answer':
                icon = 'chat-text';
                bgColor = 'bg-info';
                headerText = 'New Answer';
                break;
            case 'Accept':
                icon = 'check-circle';
                bgColor = 'bg-success';
                headerText = 'Answer Accepted';
                break;
            case 'Comment':
                icon = 'chat-dots';
                bgColor = 'bg-secondary';
                headerText = 'New Comment';
                break;
            case 'Mention':
                icon = 'at';
                bgColor = 'bg-warning';
                headerText = 'Mention';
                break;
        }
        
        // Create toast HTML
        const toastHtml = `
            <div id="${toastId}" class="toast" role="alert" aria-live="assertive" aria-atomic="true" data-bs-delay="8000">
                <div class="toast-header ${bgColor} text-white">
                    <i class="bi bi-${icon} me-2"></i>
                    <strong class="me-auto">${headerText}</strong>
                    <small>${timeAgo(notification.createdDate)}</small>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
                <div class="toast-body d-flex flex-column">
                    <div>${notification.message}</div>
                    <div class="mt-2 pt-2 border-top d-flex justify-content-end">
                        <a href="${notification.url}" class="btn btn-sm btn-primary me-1">View</a>
                        <button type="button" class="btn btn-sm btn-secondary" 
                            onclick="NotificationHandler.markAsRead(${notification.id})">
                            Mark as Read
                        </button>
                    </div>
                </div>
            </div>
        `;
        
        // Add toast to container
        container.insertAdjacentHTML('beforeend', toastHtml);
        
        // Initialize the Bootstrap toast and show it
        const toastElement = document.getElementById(toastId);
        const toast = new bootstrap.Toast(toastElement);
        toast.show();
        
        // Remove toast from DOM after it's hidden
        toastElement.addEventListener('hidden.bs.toast', () => {
            toastElement.remove();
        });

        // Also play a notification sound if available
        playNotificationSound(notification.type);
    }
    
    // Play notification sound based on type
    function playNotificationSound(notificationType) {
        // If sound is disabled in browser or user preferences, skip
        const soundEnabled = localStorage.getItem('notification_sound_enabled');
        if (soundEnabled === 'false') {
            return;
        }
        
        let soundUrl = '';
        
        // Set different sounds for different notification types
        switch (notificationType) {
            case 'Vote':
                soundUrl = '/sounds/upvote-notification.mp3';
                break;
            case 'Answer':
                soundUrl = '/sounds/answer-notification.mp3';
                break;
            default:
                soundUrl = '/sounds/notification.mp3';
                break;
        }
        
        // Try to play the sound if it exists
        try {
            // First check if the sound file exists
            fetch(soundUrl)
                .then(response => {
                    if (response.ok) {
                        const audio = new Audio(soundUrl);
                        audio.volume = 0.5; // 50% volume
                        const playPromise = audio.play();
                        
                        // Handle promise to avoid uncaught errors
                        if (playPromise !== undefined) {
                            playPromise.catch(error => {
                                // Auto-play was prevented
                                console.log("Auto-play prevented for notification sound:", error);
                            });
                        }
                    } else {
                        // If sound file doesn't exist, play a default beep
                        console.log("Notification sound file not found, using fallback");
                        const beep = new Audio("data:audio/wav;base64,UklGRl9vT19XQVZFZm10IBAAAAABAAEAQB8AAEAfAAABAAgAZGF0YU");
                        beep.volume = 0.3;
                        beep.play().catch(e => console.log("Couldn't play fallback sound"));
                    }
                })
                .catch(error => {
                    console.log("Error checking sound file:", error);
                });
        } catch (error) {
            console.log("Error playing notification sound:", error);
        }
    }
    
    // Format date as time ago
    function timeAgo(dateString) {
        const date = new Date(dateString);
        const now = new Date();
        const seconds = Math.floor((now - date) / 1000);
        
        if (seconds < 60) {
            return 'just now';
        }
        
        const minutes = Math.floor(seconds / 60);
        if (minutes < 60) {
            return `${minutes}m ago`;
        }
        
        const hours = Math.floor(minutes / 60);
        if (hours < 24) {
            return `${hours}h ago`;
        }
        
        const days = Math.floor(hours / 24);
        if (days < 30) {
            return `${days}d ago`;
        }
        
        // Format as regular date for older notifications
        return date.toLocaleDateString();
    }
    
    // Update notification badge
    function updateNotificationBadge(count) {
        const badge = document.querySelector('.notification-badge');
        if (badge) {
            notificationCount = count;
            if (count > 0) {
                badge.textContent = count > 99 ? '99+' : count.toString();
                badge.style.display = 'inline-block';
            } else {
                badge.textContent = '0';
                badge.style.display = 'none';
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
    
    // Add sound toggle to settings dropdown if it exists
    function addSoundToggleToSettings() {
        const settingsDropdown = document.querySelector('.dropdown-menu');
        if (!settingsDropdown) return;

        // Check if sound is enabled (default to true if not set)
        const soundEnabled = localStorage.getItem('notification_sound_enabled') !== 'false';
        
        // Create the toggle element
        const soundToggle = document.createElement('div');
        soundToggle.className = 'dropdown-item d-flex align-items-center justify-content-between notification-sound-toggle';
        soundToggle.innerHTML = `
            <span>Notification Sounds</span>
            <div class="form-check form-switch">
                <input class="form-check-input" type="checkbox" id="notification-sound-toggle" 
                    ${soundEnabled ? 'checked' : ''}>
            </div>
        `;
        
        // Add a divider before the toggle
        const divider = document.createElement('div');
        divider.className = 'dropdown-divider';
        
        // Insert into dropdown
        const logoutItem = settingsDropdown.querySelector('form#logoutForm');
        if (logoutItem) {
            settingsDropdown.insertBefore(divider, logoutItem);
            settingsDropdown.insertBefore(soundToggle, logoutItem);
        } else {
            settingsDropdown.appendChild(divider);
            settingsDropdown.appendChild(soundToggle);
        }
        
        // Add event listener to toggle
        document.getElementById('notification-sound-toggle')?.addEventListener('change', function() {
            localStorage.setItem('notification_sound_enabled', this.checked);
            console.log(`Notification sounds ${this.checked ? 'enabled' : 'disabled'}`);
        });
    }
    
    // Add notification to dropdown
    function addNotificationToDropdown(notification) {
        // Get notification list container
        const dropdownList = document.querySelector('.notification-list');
        if (!dropdownList) {
            console.error("Notification dropdown list not found");
            return;
        }
        
        // Determine icon based on notification type
        let iconClass = 'bell';
        switch (notification.type) {
            case 'Vote':
                iconClass = 'arrow-up-circle';
                break;
            case 'Answer':
                iconClass = 'chat-text';
                break;
            case 'Accept':
                iconClass = 'check-circle';
                break;
            case 'Comment':
                iconClass = 'chat-dots';
                break;
            case 'Mention':
                iconClass = 'at';
                break;
        }
        
        // Create notification item element
        const notificationItem = document.createElement('a');
        notificationItem.href = notification.url;
        notificationItem.className = 'notification-item unread d-flex align-items-center';
        notificationItem.dataset.notificationId = notification.id;
        
        notificationItem.innerHTML = `
            <div class="notification-icon me-3">
                <i class="bi bi-${iconClass} fs-5"></i>
            </div>
            <div class="flex-grow-1">
                <div class="notification-title">${notification.title}</div>
                <div class="notification-message text-muted">${notification.message}</div>
                <div class="notification-time">${timeAgo(notification.createdDate)}</div>
            </div>
            <button class="btn btn-sm mark-read-btn" onclick="event.preventDefault(); NotificationHandler.markAsRead(${notification.id});">
                <i class="bi bi-check-circle"></i>
            </button>
        `;
        
        // Insert at the top of the list
        if (dropdownList.firstChild) {
            dropdownList.insertBefore(notificationItem, dropdownList.firstChild);
        } else {
            dropdownList.appendChild(notificationItem);
        }
        
        // If we have too many notifications, remove the oldest ones
        const maxNotifications = 10;
        const notificationItems = dropdownList.querySelectorAll('.notification-item');
        if (notificationItems.length > maxNotifications) {
            for (let i = maxNotifications; i < notificationItems.length; i++) {
                notificationItems[i].remove();
            }
        }
        
        // Update click handler for mark all as read button
        document.querySelector('.mark-all-read')?.addEventListener('click', function(e) {
            e.preventDefault();
            NotificationHandler.markAllAsRead();
        });
    }
    
    // Initialize the notification dropdown with existing notifications
    function initializeNotificationDropdown() {
        // Get notification list and badge
        const dropdownList = document.querySelector('.notification-list');
        const badge = document.querySelector('.notification-badge');
        
        if (!dropdownList || !badge) {
            return;
        }
        
        // Clear any existing content
        dropdownList.innerHTML = '';
        
        // Fetch existing notifications
        fetch('/Notifications/GetLatest')
            .then(response => {
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                return response.json();
            })
            .then(data => {
                if (data && Array.isArray(data.notifications)) {
                    // Add each notification to the dropdown
                    data.notifications.forEach(notification => {
                        addNotificationToDropdown(notification);
                    });
                    
                    // If no notifications, add a message
                    if (data.notifications.length === 0) {
                        dropdownList.innerHTML = `
                            <div class="p-3 text-center text-muted">
                                <i class="bi bi-bell mb-2 fs-4"></i>
                                <p>No notifications yet</p>
                            </div>
                        `;
                    }
                }
            })
            .catch(error => {
                console.error('Error fetching notifications:', error);
                dropdownList.innerHTML = `
                    <div class="p-3 text-center text-muted">
                        <p>Error loading notifications</p>
                    </div>
                `;
            });
            
        // Update click handler for mark all as read button
        document.querySelector('.mark-all-read')?.addEventListener('click', function(e) {
            e.preventDefault();
            NotificationHandler.markAllAsRead();
        });
    }
    
    // Public API
    return {
        init: init,
        joinGroup: joinGroup,
        leaveGroup: leaveGroup,
        markAsRead: markAsRead,
        markAllAsRead: markAllAsRead,
        toggleSound: function(enabled) {
            localStorage.setItem('notification_sound_enabled', enabled);
            
            // Update the toggle if it exists
            const toggle = document.getElementById('notification-sound-toggle');
            if (toggle) {
                toggle.checked = enabled;
            }
        },
        isSoundEnabled: function() {
            return localStorage.getItem('notification_sound_enabled') !== 'false';
        }
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