/**
 * Presence Client - Handles user online presence tracking
 */
const PresenceManager = (function() {
    // Private variables
    let connection = null;
    let onlineUsers = [];
    let retryCount = 0;
    const maxRetries = 5;
    
    // Initialize the presence tracking system
    function init() {
        console.log("PresenceManager: Initializing...");
        
        // Only initialize if user is logged in
        if (isUserLoggedIn()) {
            initializeSignalR();
        } else {
            console.log("PresenceManager: User not logged in, skipping presence tracking");
        }
    }
    
    // Check if user is logged in
    function isUserLoggedIn() {
        return document.querySelector('.nav-item.dropdown:has(.dropdown-item[href="javascript:void(0);"][onclick*="logoutForm"])') !== null || 
               document.querySelector('form#logoutForm') !== null;
    }
    
    // Initialize SignalR connection
    function initializeSignalR() {
        try {
            console.log("PresenceManager: Initializing SignalR...");
            
            // Check if SignalR is available
            if (typeof signalR === 'undefined') {
                console.error("SignalR library not loaded! Presence tracking won't work.");
                setTimeout(initializeSignalR, 2000); // Retry after 2 seconds
                return;
            }
            
            // Create SignalR connection
            connection = new signalR.HubConnectionBuilder()
                .withUrl('/presenceHub')
                .withAutomaticReconnect([0, 2000, 5000, 10000, 30000]) // Retry policy
                .configureLogging(signalR.LogLevel.Information)
                .build();
            
            // Set up connection event handlers
            connection.onreconnecting(error => {
                console.log('Reconnecting to presence hub...', error);
                updateConnectionStatus('connecting');
            });
            
            connection.onreconnected(connectionId => {
                console.log('Reconnected to presence hub:', connectionId);
                retryCount = 0;
                updateConnectionStatus('connected');
            });
            
            connection.onclose(error => {
                console.log('Presence connection closed:', error);
                updateConnectionStatus('disconnected');
                
                // Try to reconnect if max retries not reached
                if (retryCount < maxRetries) {
                    retryCount++;
                    setTimeout(startConnection, 5000);
                }
            });
            
            // Set up user presence handlers
            connection.on('UserOnline', handleUserOnline);
            connection.on('UserOffline', handleUserOffline);
            connection.on('OnlineUsers', handleOnlineUsersList);
            connection.on('OnlineCount', handleOnlineCount);
            
            // Start the connection
            startConnection();
        } catch (error) {
            console.error('Error initializing SignalR for presence:', error);
            setTimeout(initializeSignalR, 5000);
        }
    }
    
    // Update connection status display
    function updateConnectionStatus(status) {
        const statusElements = document.querySelectorAll('.presence-status-indicator');
        if (statusElements.length === 0) return;
        
        statusElements.forEach(element => {
            element.className = `presence-status-indicator ${status}`;
            
            let statusText = '';
            switch (status) {
                case 'connected':
                    statusText = 'Connected';
                    break;
                case 'connecting':
                    statusText = 'Connecting...';
                    break;
                case 'disconnected':
                    statusText = 'Disconnected';
                    break;
            }
            
            element.setAttribute('title', `Presence: ${statusText}`);
        });
    }
    
    // Start SignalR connection
    function startConnection() {
        if (connection) {
            console.log("PresenceManager: Starting connection...");
            updateConnectionStatus('connecting');
            
            connection.start()
                .then(() => {
                    console.log('Connected to presence hub');
                    retryCount = 0;
                    updateConnectionStatus('connected');
                    
                    // Get online count
                    getOnlineCount();
                })
                .catch(error => {
                    console.error('Error connecting to presence hub:', error);
                    updateConnectionStatus('disconnected');
                    
                    // Try to reconnect if max retries not reached
                    if (retryCount < maxRetries) {
                        retryCount++;
                        console.log(`Connection attempt ${retryCount} failed. Retrying in 5 seconds...`);
                        setTimeout(startConnection, 5000);
                    }
                });
        }
    }
    
    // Get current online users count
    function getOnlineCount() {
        if (connection && connection.state === signalR.HubConnectionState.Connected) {
            connection.invoke('GetOnlineCount')
                .catch(error => {
                    console.error('Error getting online count:', error);
                });
        }
    }
    
    // Handle user coming online
    function handleUserOnline(user) {
        console.log('User came online:', user);
        
        // Add to online users array if not already there
        if (!onlineUsers.includes(user.userId)) {
            onlineUsers.push(user.userId);
        }
        
        // Update UI to show user as online
        updateUserPresenceIndicators(user.userId, true);
    }
    
    // Handle user going offline
    function handleUserOffline(user) {
        console.log('User went offline:', user);
        
        // Remove from online users array
        const index = onlineUsers.indexOf(user.userId);
        if (index !== -1) {
            onlineUsers.splice(index, 1);
        }
        
        // Update UI to show user as offline
        updateUserPresenceIndicators(user.userId, false);
    }
    
    // Handle initial list of online users
    function handleOnlineUsersList(userIds) {
        console.log('Received online users list:', userIds);
        onlineUsers = userIds;
        
        // Update all user indicators
        updateAllPresenceIndicators();
    }
    
    // Handle online users count update
    function handleOnlineCount(count) {
        console.log('Online users count:', count);
        
        // Update count display in UI
        const countElements = document.querySelectorAll('.online-users-count');
        countElements.forEach(element => {
            element.textContent = count;
        });
    }
    
    // Update all user presence indicators
    function updateAllPresenceIndicators() {
        // Find all user elements with presence indicators
        const indicators = document.querySelectorAll('[data-user-id]');
        
        indicators.forEach(indicator => {
            const userId = indicator.getAttribute('data-user-id');
            const isOnline = onlineUsers.includes(userId);
            updateUserPresenceIndicators(userId, isOnline);
        });
    }
    
    // Update presence indicators for a specific user
    function updateUserPresenceIndicators(userId, isOnline) {
        // Find all elements for this user
        const elements = document.querySelectorAll(`[data-user-id="${userId}"]`);
        
        elements.forEach(element => {
            // Remove existing status classes
            element.classList.remove('user-online', 'user-offline');
            
            // Add new status class
            element.classList.add(isOnline ? 'user-online' : 'user-offline');
            
            // Update presence indicator if it exists
            const indicator = element.querySelector('.presence-indicator');
            if (indicator) {
                indicator.setAttribute('title', isOnline ? 'Online' : 'Offline');
                indicator.classList.remove('online', 'offline');
                indicator.classList.add(isOnline ? 'online' : 'offline');
            }
        });
    }
    
    // Check if a specific user is online
    function isUserOnline(userId) {
        return onlineUsers.includes(userId);
    }
    
    // Public API
    return {
        init: init,
        isUserOnline: isUserOnline,
        getOnlineCount: getOnlineCount
    };
})();

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    try {
        // Wait for everything to load
        setTimeout(() => {
            PresenceManager.init();
        }, 1000);
    } catch (error) {
        console.error("Error initializing PresenceManager:", error);
    }
}); 