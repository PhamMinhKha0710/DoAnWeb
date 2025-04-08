/**
 * Badge Progress Client
 * 
 * This script handles real-time updates to badge progress using SignalR.
 */

// Self-executing anonymous function
(function () {
    // Private variables
    let badgeHubConnection = null;
    let isConnected = false;
    let retryCount = 0;
    const maxRetries = 5;
    const badgeProgressContainer = document.getElementById("badge-progress-container");
    
    // Initialize the badge progress client
    function initializeBadgeClient() {
        if (!shouldInitialize()) return;
        
        console.log("Initializing badge progress client...");
        
        // Wait for SignalR to be loaded
        if (typeof signalR === 'undefined') {
            console.log("SignalR not loaded yet, waiting...");
            setTimeout(initializeBadgeClient, 1000);
            return;
        }
        
        createConnection();
        setupEventHandlers();
        startConnection();
    }
    
    // Check if we should initialize the badge client
    function shouldInitialize() {
        // Only initialize if:
        // 1. We're on a page with the badge progress container
        // 2. The user is authenticated (we assume the badge container only shows for authenticated users)
        return badgeProgressContainer !== null;
    }
    
    // Create the SignalR connection
    function createConnection() {
        console.log("Creating SignalR connection to BadgeHub...");
        
        badgeHubConnection = new signalR.HubConnectionBuilder()
            .withUrl("/badgeHub")
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000]) // Retry policy
            .configureLogging(signalR.LogLevel.Information)
            .build();
    }
    
    // Set up event handlers for the hub connection
    function setupEventHandlers() {
        console.log("Setting up badge hub event handlers...");
        
        // Reconnecting event
        badgeHubConnection.onreconnecting(error => {
            console.log("Reconnecting to badge hub...", error);
            isConnected = false;
            updateConnectionStatus("connecting");
        });
        
        // Reconnected event
        badgeHubConnection.onreconnected(connectionId => {
            console.log("Reconnected to badge hub:", connectionId);
            isConnected = true;
            updateConnectionStatus("connected");
        });
        
        // Disconnected event
        badgeHubConnection.onclose(error => {
            console.log("Disconnected from badge hub:", error);
            isConnected = false;
            updateConnectionStatus("disconnected");
            
            // Attempt to reconnect if not a deliberate close
            if (retryCount < maxRetries) {
                retryCount++;
                setTimeout(() => {
                    startConnection();
                }, 2000 * retryCount); // Increasing backoff
            }
        });
        
        // Badge progress updated event
        badgeHubConnection.on("BadgeProgressUpdated", handleBadgeProgressUpdate);
        
        // Badge awarded event
        badgeHubConnection.on("BadgeAwarded", handleBadgeAwarded);
    }
    
    // Start the connection to the hub
    function startConnection() {
        if (!badgeHubConnection) return;
        
        console.log("Starting badge hub connection...");
        updateConnectionStatus("connecting");
        
        badgeHubConnection.start()
            .then(() => {
                console.log("Connected to badge hub successfully");
                isConnected = true;
                retryCount = 0;
                updateConnectionStatus("connected");
            })
            .catch(err => {
                console.error("Error connecting to badge hub:", err);
                updateConnectionStatus("error");
                
                // Retry connection with backoff
                if (retryCount < maxRetries) {
                    retryCount++;
                    console.log(`Will retry connection in ${2 * retryCount} seconds...`);
                    setTimeout(() => {
                        startConnection();
                    }, 2000 * retryCount);
                }
            });
    }
    
    // Update UI to reflect connection status (optional visual indicator)
    function updateConnectionStatus(status) {
        // You could add a small indicator somewhere on the page
        console.log(`Badge hub connection status: ${status}`);
    }
    
    // Handle badge progress update from the server
    function handleBadgeProgressUpdate(badgeProgress) {
        console.log("Received badge progress update:", badgeProgress);
        
        if (!badgeProgress || !badgeProgressContainer) return;
        
        // Find existing badge progress element or create a new one
        let badgeElement = findOrCreateBadgeElement(badgeProgress);
        
        // Update badge progress content
        updateBadgeProgressContent(badgeElement, badgeProgress);
        
        // Apply animation to highlight the update
        animateBadgeUpdate(badgeElement);
    }
    
    // Find existing badge element or create a new one
    function findOrCreateBadgeElement(badgeProgress) {
        // Check if this badge is already in the container
        let badgeElement = document.querySelector(`.badge-progress[data-badge-id="${badgeProgress.badgeId}"]`);
        
        // If not found, create a new element
        if (!badgeElement) {
            badgeElement = document.createElement('div');
            badgeElement.className = 'badge-progress mb-3';
            badgeElement.setAttribute('data-badge-id', badgeProgress.badgeId);
            badgeElement.innerHTML = `
                <div class="d-flex justify-content-between align-items-center mb-2">
                    <div class="d-flex align-items-center">
                        <div class="badge-icon rounded-circle ${badgeProgress.colorClass} p-2 me-2 d-flex align-items-center justify-content-center" style="width: 40px; height: 40px;">
                            <i class="bi ${badgeProgress.iconClass} text-white"></i>
                        </div>
                        <div>
                            <h6 class="fw-bold mb-0">${badgeProgress.name}</h6>
                            <small class="text-muted">${badgeProgress.description}</small>
                        </div>
                    </div>
                    <span class="badge bg-primary rounded-pill progress-count">${badgeProgress.currentCount}/${badgeProgress.targetCount}</span>
                </div>
                <div class="progress" style="height: 8px;">
                    <div class="progress-bar ${badgeProgress.colorClass}" role="progressbar" 
                         style="width: ${calculateProgressPercentage(badgeProgress.currentCount, badgeProgress.targetCount)}%;" 
                         aria-valuenow="${badgeProgress.currentCount}" 
                         aria-valuemin="0" 
                         aria-valuemax="${badgeProgress.targetCount}"></div>
                </div>
            `;
            
            // Add to container
            badgeProgressContainer.appendChild(badgeElement);
        }
        
        return badgeElement;
    }
    
    // Update badge progress content
    function updateBadgeProgressContent(badgeElement, badgeProgress) {
        // Update data attributes for tracking
        badgeElement.setAttribute('data-current-count', badgeProgress.currentCount);
        badgeElement.setAttribute('data-target-count', badgeProgress.targetCount);
        
        // Update progress text
        const progressCountElement = badgeElement.querySelector('.progress-count');
        if (progressCountElement) {
            progressCountElement.textContent = `${badgeProgress.currentCount}/${badgeProgress.targetCount}`;
        }
        
        // Update progress bar
        const progressBar = badgeElement.querySelector('.progress-bar');
        if (progressBar) {
            const progressPercentage = calculateProgressPercentage(badgeProgress.currentCount, badgeProgress.targetCount);
            progressBar.style.width = `${progressPercentage}%`;
            progressBar.setAttribute('aria-valuenow', badgeProgress.currentCount);
        }
    }
    
    // Calculate progress percentage
    function calculateProgressPercentage(current, target) {
        if (target <= 0) return 0;
        return Math.min(100, Math.round((current / target) * 100));
    }
    
    // Animate badge update to draw attention
    function animateBadgeUpdate(badgeElement) {
        // Add a highlight class
        badgeElement.classList.add('badge-updated');
        
        // Animate the badge icon
        const iconElement = badgeElement.querySelector('.badge-icon');
        if (iconElement) {
            iconElement.classList.add('badge-icon-pulse');
            
            // Remove animation classes after animation completes
            setTimeout(() => {
                badgeElement.classList.remove('badge-updated');
                iconElement.classList.remove('badge-icon-pulse');
            }, 2000);
        }
    }
    
    // Handle badge awarded event
    function handleBadgeAwarded(badgeInfo) {
        console.log("Badge awarded:", badgeInfo);
        
        // Show a toast or notification
        showBadgeAwardedNotification(badgeInfo);
        
        // Refresh badges (could be done by requesting a recalculation from the server)
        // This is optional since the server will send updated progress anyway
    }
    
    // Show a notification when a badge is awarded
    function showBadgeAwardedNotification(badgeInfo) {
        // Create toast notification element
        const toastElement = document.createElement('div');
        toastElement.className = 'toast badge-toast show';
        toastElement.setAttribute('role', 'alert');
        toastElement.setAttribute('aria-live', 'assertive');
        toastElement.setAttribute('aria-atomic', 'true');
        
        // Add toast content
        toastElement.innerHTML = `
            <div class="toast-header">
                <strong class="me-auto">Badge Earned!</strong>
                <small>Just now</small>
                <button type="button" class="btn-close" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
            <div class="toast-body d-flex align-items-center">
                <div class="badge-icon-sm rounded-circle bg-primary me-2 d-flex align-items-center justify-content-center">
                    <i class="bi bi-award-fill text-white"></i>
                </div>
                <div>
                    <strong>${badgeInfo.name}</strong>
                    <p class="mb-0 small">${badgeInfo.description}</p>
                    <p class="mb-0 small text-success">+${badgeInfo.reputationBonus} reputation points</p>
                </div>
            </div>
        `;
        
        // Add to page
        document.body.appendChild(toastElement);
        
        // Remove after 5 seconds
        setTimeout(() => {
            toastElement.classList.remove('show');
            setTimeout(() => {
                document.body.removeChild(toastElement);
            }, 500);
        }, 5000);
    }
    
    // Initialize when the document is ready
    document.addEventListener('DOMContentLoaded', initializeBadgeClient);
})(); 