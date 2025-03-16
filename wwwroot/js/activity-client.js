/**
 * Activity Client - Tracks and displays real-time site activity
 */
const ActivityTracker = (function() {
    // Private variables
    let connection = null;
    let activities = [];
    let retryCount = 0;
    const maxRetries = 3;
    
    // Initialize the activity tracking system
    function init() {
        console.log("ActivityTracker: Initializing...");
        initializeSignalR();
        setupActivityFeeds();
        
        // Track page changes
        window.addEventListener('popstate', trackCurrentPage);
        
        // Track initial page load
        setTimeout(trackCurrentPage, 1000);
    }
    
    // Initialize SignalR connection
    function initializeSignalR() {
        try {
            console.log("ActivityTracker: Initializing SignalR...");
            
            // Check if SignalR is available
            if (typeof signalR === 'undefined') {
                console.error("SignalR library not loaded! Activity tracking won't work.");
                setTimeout(initializeSignalR, 2000); // Retry after 2 seconds
                return;
            }
            
            // Create SignalR connection
            connection = new signalR.HubConnectionBuilder()
                .withUrl('/activityHub')
                .withAutomaticReconnect([0, 2000, 5000, 10000]) // Retry policy
                .configureLogging(signalR.LogLevel.Information)
                .build();
            
            // Set up connection event handlers
            connection.onreconnecting(error => {
                console.log('Reconnecting to activity hub...', error);
            });
            
            connection.onreconnected(connectionId => {
                console.log('Reconnected to activity hub:', connectionId);
                retryCount = 0;
                
                // Re-track current page
                trackCurrentPage();
            });
            
            connection.onclose(error => {
                console.log('Activity connection closed:', error);
                
                // Try to reconnect if max retries not reached
                if (retryCount < maxRetries) {
                    retryCount++;
                    setTimeout(startConnection, 5000);
                }
            });
            
            // Set up activity event handlers
            connection.on('ActivityStats', handleActivityStats);
            connection.on('NewPageView', handleNewPageView);
            connection.on('ActivityAction', handleActivityAction);
            connection.on('DetailedActivity', handleDetailedActivity);
            
            // Start the connection
            startConnection();
        } catch (error) {
            console.error('Error initializing SignalR for activity tracking:', error);
        }
    }
    
    // Set up activity UI elements
    function setupActivityFeeds() {
        // Check if we need to show admin-level detailed activity
        const adminFeeds = document.querySelectorAll('.admin-activity-feed');
        if (adminFeeds.length > 0) {
            // Subscribe to detailed feed when connection established
            if (connection && connection.state === signalR.HubConnectionState.Connected) {
                subscribeToDetailedFeed();
            } else {
                // Will try again when connection is ready
                console.log("Will subscribe to detailed feed when connection is ready");
            }
        }
    }
    
    // Start SignalR connection
    function startConnection() {
        if (connection) {
            console.log("ActivityTracker: Starting connection...");
            
            connection.start()
                .then(() => {
                    console.log('Connected to activity hub');
                    retryCount = 0;
                    
                    // Track current page
                    trackCurrentPage();
                    
                    // Subscribe to detailed feed if admin
                    const adminFeeds = document.querySelectorAll('.admin-activity-feed');
                    if (adminFeeds.length > 0) {
                        subscribeToDetailedFeed();
                    }
                })
                .catch(error => {
                    console.error('Error connecting to activity hub:', error);
                    
                    // Try to reconnect if max retries not reached
                    if (retryCount < maxRetries) {
                        retryCount++;
                        console.log(`Connection attempt ${retryCount} failed. Retrying in 5 seconds...`);
                        setTimeout(startConnection, 5000);
                    }
                });
        }
    }
    
    // Subscribe to detailed activity feed
    function subscribeToDetailedFeed() {
        if (connection && connection.state === signalR.HubConnectionState.Connected) {
            connection.invoke('SubscribeToDetailedFeed')
                .catch(error => {
                    console.error('Error subscribing to detailed feed:', error);
                });
        }
    }
    
    // Track the current page view
    function trackCurrentPage() {
        if (connection && connection.state === signalR.HubConnectionState.Connected) {
            const path = window.location.pathname + window.location.search;
            const title = document.title;
            
            connection.invoke('PageView', path, title)
                .catch(error => {
                    console.error('Error tracking page view:', error);
                });
        }
    }
    
    // Record a specific user action
    function recordAction(actionType, targetId, details) {
        if (connection && connection.state === signalR.HubConnectionState.Connected) {
            connection.invoke('RecordAction', actionType, targetId, details)
                .catch(error => {
                    console.error('Error recording action:', error);
                });
        }
    }
    
    // Handle site activity statistics
    function handleActivityStats(stats) {
        console.log('Activity stats:', stats);
        
        // Update active users count
        const activeUsersElements = document.querySelectorAll('.active-users-count');
        activeUsersElements.forEach(element => {
            element.textContent = stats.activeConnections;
        });
        
        // Update page views count
        const pageViewsElements = document.querySelectorAll('.page-views-count');
        pageViewsElements.forEach(element => {
            element.textContent = stats.totalPageViews;
        });
        
        // Update timestamp
        const timestampElements = document.querySelectorAll('.activity-timestamp');
        timestampElements.forEach(element => {
            const date = new Date(stats.timestamp);
            element.textContent = date.toLocaleTimeString();
        });
    }
    
    // Handle new page view
    function handleNewPageView(pageView) {
        console.log('New page view:', pageView);
        
        // Add to activity feed
        addActivityToFeed({
            type: 'page-view',
            message: `Someone viewed ${pageView.pageTitle}`,
            timestamp: pageView.timestamp
        });
    }
    
    // Handle general site activity
    function handleActivityAction(action) {
        console.log('Activity action:', action);
        
        // Create readable message based on action type
        let message = "Someone did something";
        switch (action.actionType) {
            case 'question':
                message = "Someone posted a new question";
                break;
            case 'answer':
                message = "Someone posted a new answer";
                break;
            case 'comment':
                message = "Someone added a comment";
                break;
            case 'vote':
                message = "Someone voted on content";
                break;
            case 'registration':
                message = "A new user registered";
                break;
            default:
                message = `Activity: ${action.actionType}`;
        }
        
        // Add to activity feed
        addActivityToFeed({
            type: action.actionType,
            message: message,
            timestamp: action.timestamp
        });
    }
    
    // Handle detailed activity for admins
    function handleDetailedActivity(activity) {
        console.log('Detailed activity:', activity);
        
        // Add to admin activity feeds
        const adminFeeds = document.querySelectorAll('.admin-activity-feed');
        if (adminFeeds.length === 0) return;
        
        // Create detailed activity item
        const date = new Date(activity.timestamp);
        const timeString = date.toLocaleTimeString();
        
        const activityItem = document.createElement('div');
        activityItem.className = `activity-item activity-${activity.actionType}`;
        activityItem.innerHTML = `
            <div class="activity-time">${timeString}</div>
            <div class="activity-details">
                <strong>${activity.actionType}</strong>
                <span>Target: ${activity.targetId}</span>
                <span>${activity.details}</span>
            </div>
        `;
        
        // Add to each admin feed
        adminFeeds.forEach(feed => {
            // Insert at top
            if (feed.firstChild) {
                feed.insertBefore(activityItem.cloneNode(true), feed.firstChild);
            } else {
                feed.appendChild(activityItem.cloneNode(true));
            }
            
            // Limit items
            limitFeedItems(feed);
        });
    }
    
    // Add activity to feed displays
    function addActivityToFeed(activity) {
        // Add to memory
        activities.unshift(activity);
        if (activities.length > 100) {
            activities.pop();
        }
        
        // Add to DOM feeds
        const feeds = document.querySelectorAll('.activity-feed');
        if (feeds.length === 0) return;
        
        // Format time
        const date = new Date(activity.timestamp);
        const timeString = date.toLocaleTimeString();
        
        // Create activity element
        const activityItem = document.createElement('div');
        activityItem.className = `activity-item activity-${activity.type}`;
        activityItem.innerHTML = `
            <div class="activity-time">${timeString}</div>
            <div class="activity-message">${activity.message}</div>
        `;
        
        // Add animation class
        activityItem.classList.add('activity-new');
        
        // Add to each feed
        feeds.forEach(feed => {
            // Insert at top
            if (feed.firstChild) {
                feed.insertBefore(activityItem.cloneNode(true), feed.firstChild);
            } else {
                feed.appendChild(activityItem.cloneNode(true));
            }
            
            // Limit items
            limitFeedItems(feed);
        });
        
        // Remove animation class after animation completes
        setTimeout(() => {
            document.querySelectorAll('.activity-new').forEach(item => {
                item.classList.remove('activity-new');
            });
        }, 2000);
    }
    
    // Limit the number of items in a feed
    function limitFeedItems(feed) {
        const maxItems = feed.getAttribute('data-max-items') || 10;
        const items = feed.querySelectorAll('.activity-item');
        
        if (items.length > maxItems) {
            for (let i = maxItems; i < items.length; i++) {
                feed.removeChild(items[i]);
            }
        }
    }
    
    // Public API
    return {
        init: init,
        recordAction: recordAction
    };
})();

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    try {
        // Wait for everything to load
        setTimeout(() => {
            ActivityTracker.init();
            
            // Set up action tracking for common interactions
            setupActionTracking();
        }, 1000);
    } catch (error) {
        console.error("Error initializing ActivityTracker:", error);
    }
});

// Set up tracking for various user actions
function setupActionTracking() {
    // Track clicks on questions
    document.addEventListener('click', function(e) {
        const questionLink = e.target.closest('a[href^="/Question/Details/"]');
        if (questionLink) {
            const questionId = questionLink.getAttribute('href').split('/').pop();
            ActivityTracker.recordAction('question-view', questionId, 'Viewed question');
        }
    });
    
    // Track votes (likes, helpful, etc.)
    document.addEventListener('click', function(e) {
        const voteButton = e.target.closest('.vote-button, .like-button');
        if (voteButton) {
            const contentId = voteButton.getAttribute('data-id');
            const contentType = voteButton.getAttribute('data-type');
            ActivityTracker.recordAction('vote', contentId, `Voted on ${contentType}`);
        }
    });
    
    // Track form submissions
    document.addEventListener('submit', function(e) {
        const form = e.target;
        if (form.id === 'question-form') {
            ActivityTracker.recordAction('question', 'new', 'Posted new question');
        } else if (form.id === 'answer-form') {
            const questionId = form.getAttribute('data-question-id');
            ActivityTracker.recordAction('answer', questionId, 'Posted new answer');
        } else if (form.id.startsWith('comment-form')) {
            const targetId = form.getAttribute('data-target-id');
            ActivityTracker.recordAction('comment', targetId, 'Posted new comment');
        }
    });
} 