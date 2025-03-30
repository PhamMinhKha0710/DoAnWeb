/**
 * SignalR Connection Checker
 * Script để theo dõi và ghi log trạng thái của tất cả kết nối SignalR
 * Cập nhật để quản lý tất cả các hub
 */

document.addEventListener('DOMContentLoaded', function() {
    console.log("SignalR Connection Checker starting...");

    // Sử dụng SignalRLoader để đảm bảo SignalR đã được tải trước khi khởi tạo kết nối
    if (window.SignalRLoader) {
        SignalRLoader.ready(function() {
            console.log("SignalR loaded successfully, initializing connection checker");
            
            // Khởi tạo tất cả các kết nối cần thiết
            if (isUserLoggedIn()) {
                ensureNotificationConnection();
                ensureViewCountConnection();
                ensureQuestionHubConnection();
                ensurePresenceHubConnection();
                ensureChatHubConnection();
                ensureActivityHubConnection();
            } else {
                // Nếu người dùng chưa đăng nhập, chỉ thiết lập các hub không yêu cầu xác thực
                ensureViewCountConnection();
            }
            
            // Kiểm tra sau 2 giây để cho các connection thời gian khởi tạo
            setTimeout(checkAllConnections, 2000);
        });
    } else {
        console.error("SignalRLoader not found. Make sure signalr-loader.js is loaded first.");
    }
});

/**
 * Kiểm tra xem người dùng đã đăng nhập hay chưa
 */
function isUserLoggedIn() {
    // Kiểm tra biểu mẫu đăng xuất (một cách đáng tin cậy để xác định đăng nhập)
    return document.querySelector('form#logoutForm') !== null ||
           document.querySelector('.nav-item.dropdown:has(.dropdown-item[onclick*="logoutForm"])') !== null ||
           document.querySelector('.dropdown-item[onclick*="logoutForm"]') !== null;
}

/**
 * Tạo kết nối hub base với cách xử lý lỗi và retry phù hợp
 * @param {string} hubUrl - URL đến hub
 * @param {string} hubName - Tên của hub để hiển thị trong log
 * @param {boolean} requiresAuth - Hub có yêu cầu người dùng đăng nhập không
 * @returns {object} - SignalR hub connection hoặc null nếu không thể tạo
 */
function createHubConnection(hubUrl, hubName, requiresAuth = true) {
    // Kiểm tra SignalR đã load
    if (typeof signalR === 'undefined') {
        console.error(`SignalR library not loaded! Cannot initialize ${hubName} connection.`);
        return null;
    }

    // Kiểm tra xác thực nếu cần
    if (requiresAuth && !isUserLoggedIn()) {
        console.log(`User not logged in. ${hubName} requires authentication. Skipping connection setup.`);
        return null;
    }

    try {
        // Tạo kết nối mới
        const connection = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl)
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Information)
            .build();

        // Thiết lập các event handler cơ bản
        connection.onreconnecting(error => {
            console.log(`Reconnecting to ${hubName}...`, error);
            updateConnectionStatusIndicator(hubName, "Connecting");
        });

        connection.onreconnected(connectionId => {
            console.log(`Reconnected to ${hubName}:`, connectionId);
            updateConnectionStatusIndicator(hubName, "Connected");
        });

        connection.onclose(error => {
            console.log(`Disconnected from ${hubName}:`, error);
            updateConnectionStatusIndicator(hubName, "Disconnected");
        });

        return connection;
    } catch (error) {
        console.error(`Error setting up ${hubName} connection:`, error);
        return null;
    }
}

/**
 * Bắt đầu kết nối SignalR với retry logic 
 * @param {object} connection - SignalR connection object
 * @param {string} hubName - Tên hub để hiển thị trong logs
 */
function startHubConnection(connection, hubName) {
    if (!connection) return;
    
    console.log(`Starting ${hubName} connection...`);
    updateConnectionStatusIndicator(hubName, "Connecting");
    
    connection.start()
        .then(() => {
            console.log(`Connected to ${hubName}`);
            updateConnectionStatusIndicator(hubName, "Connected");
        })
        .catch(error => {
            console.error(`Error connecting to ${hubName}:`, error);
            updateConnectionStatusIndicator(hubName, "Disconnected");
            
            // Kiểm tra lỗi - có thể do chưa đăng nhập
            if (error && error.message && error.message.includes("401")) {
                console.log(`Authentication error - user may not be logged in for ${hubName}`);
            } else {
                // Thử kết nối lại sau 5 giây cho các lỗi khác
                setTimeout(() => startHubConnection(connection, hubName), 5000);
            }
        });
}

/**
 * Đảm bảo kết nối NotificationHub luôn tồn tại
 */
function ensureNotificationConnection() {
    if (!window.notificationConnection) {
        console.log("NotificationHub connection not found, initializing...");
        
        window.notificationConnection = createHubConnection('/notificationHub', 'NotificationHub', true);
        
        if (window.notificationConnection) {
            // Đăng ký handler cho thông báo
            window.notificationConnection.on("ReceiveNotification", (notification) => {
                console.log("Notification received:", notification);
                // Basic notification display
                if (window.showNotificationToast) {
                    window.showNotificationToast(notification);
                }
            });
            
            // Bắt đầu kết nối
            startHubConnection(window.notificationConnection, 'NotificationHub');
        }
    }
}

/**
 * Đảm bảo kết nối ViewCountHub luôn tồn tại
 */
function ensureViewCountConnection() {
    if (!window.viewCountConnection) {
        console.log("ViewCountHub connection not found, initializing...");
        
        // ViewCountHub không yêu cầu xác thực, có thể dùng cho người dùng ẩn danh
        window.viewCountConnection = createHubConnection('/viewCountHub', 'ViewCountHub', false);
        
        if (window.viewCountConnection) {
            // Đăng ký các handler cơ bản
            window.viewCountConnection.on("ReceiveUpdatedViewCount", function(qId, viewCount) {
                console.log(`ViewCountHub: Received updated view count for question ${qId}: ${viewCount}`);
            });
            
            window.viewCountConnection.on("ReceiveCurrentViewCount", function(qId, viewCount) {
                console.log(`ViewCountHub: Received current view count for question ${qId}: ${viewCount}`);
            });
            
            // Bắt đầu kết nối
            startHubConnection(window.viewCountConnection, 'ViewCountHub');
        }
    }
}

/**
 * Đảm bảo kết nối QuestionHub luôn tồn tại
 */
function ensureQuestionHubConnection() {
    if (!window.questionHubConnection) {
        console.log("QuestionHub connection not found, initializing...");
        
        window.questionHubConnection = createHubConnection('/questionHub', 'QuestionHub', true);
        
        if (window.questionHubConnection) {
            // Đăng ký handler mặc định
            window.questionHubConnection.on("ReceiveQuestionUpdate", function(question) {
                console.log("Question update received:", question);
            });
            
            // Bắt đầu kết nối
            startHubConnection(window.questionHubConnection, 'QuestionHub');
        }
    }
}

/**
 * Đảm bảo kết nối PresenceHub luôn tồn tại
 */
function ensurePresenceHubConnection() {
    if (!window.presenceConnection) {
        console.log("PresenceHub connection not found, initializing...");
        
        window.presenceConnection = createHubConnection('/presenceHub', 'PresenceHub', true);
        
        if (window.presenceConnection) {
            // Đăng ký handler mặc định cho presence
            window.presenceConnection.on("UserOnline", function(user) {
                console.log("User online:", user);
            });
            
            window.presenceConnection.on("UserOffline", function(user) {
                console.log("User offline:", user);
            });
            
            // Thêm các handler cần thiết để tránh cảnh báo
            window.presenceConnection.on("OnlineUsers", function(users) {
                console.log("Online users list received:", users);
            });
            
            window.presenceConnection.on("OnlineCount", function(count) {
                console.log("Online count:", count);
            });
            
            // Bắt đầu kết nối
            startHubConnection(window.presenceConnection, 'PresenceHub');
        }
    }
}

/**
 * Đảm bảo kết nối ChatHub luôn tồn tại
 */
function ensureChatHubConnection() {
    if (!window.chatConnection) {
        console.log("ChatHub connection not found, initializing...");
        
        window.chatConnection = createHubConnection('/chatHub', 'ChatHub', true);
        
        if (window.chatConnection) {
            // Đăng ký handler mặc định cho chat
            window.chatConnection.on("ReceiveMessage", function(user, message) {
                console.log(`Chat message from ${user}: ${message}`);
            });
            
            // Bắt đầu kết nối
            startHubConnection(window.chatConnection, 'ChatHub');
        }
    }
}

/**
 * Đảm bảo kết nối ActivityHub luôn tồn tại
 */
function ensureActivityHubConnection() {
    if (!window.activityConnection) {
        console.log("ActivityHub connection not found, initializing...");
        
        window.activityConnection = createHubConnection('/activityHub', 'ActivityHub', true);
        
        if (window.activityConnection) {
            // Đăng ký handler mặc định cho activity
            window.activityConnection.on("ReceiveActivity", function(activity) {
                console.log("Activity received:", activity);
            });
            
            // Thêm các handler cần thiết để tránh cảnh báo
            window.activityConnection.on("ActivityStats", function(stats) {
                console.log("ActivityStats received:", stats);
            });
            
            window.activityConnection.on("OnlineUsers", function(users) {
                console.log("Online users received:", users);
            });
            
            window.activityConnection.on("NewPageView", function(pageView) {
                console.log("New page view:", pageView);
            });
            
            // Bắt đầu kết nối
            startHubConnection(window.activityConnection, 'ActivityHub');
        }
    }
}

/**
 * Cập nhật trạng thái kết nối trên UI nếu có
 */
function updateConnectionStatusIndicator(hubName, state) {
    const indicator = document.querySelector('.connection-status-indicator');
    if (indicator) {
        indicator.className = 'connection-status-indicator';
        indicator.classList.add(`status-${state.toLowerCase()}`);
        indicator.title = `${hubName}: ${state}`;
    }
}

function checkAllConnections() {
    console.group("SignalR Connection Status");
    
    // Kiểm tra NotificationHub
    if (window.notificationConnection) {
        logConnectionStatus("NotificationHub", window.notificationConnection.state);
    } else {
        // Chỉ hiển thị cảnh báo và thử tạo kết nối nếu người dùng đã đăng nhập
        if (isUserLoggedIn()) {
            console.warn("NotificationHub connection not found in global scope");
            // Thử tạo kết nối nếu chưa có
            ensureNotificationConnection();
        } else {
            console.log("NotificationHub: User not logged in (connection not required)");
        }
    }
    
    // Kiểm tra ViewCountHub
    if (window.viewCountConnection) {
        logConnectionStatus("ViewCountHub", window.viewCountConnection.state);
    } else {
        console.warn("ViewCountHub connection not found in global scope");
        ensureViewCountConnection();
    }
    
    // Kiểm tra QuestionHub
    if (window.questionHubConnection) {
        logConnectionStatus("QuestionHub", window.questionHubConnection.state);
    } else {
        if (isUserLoggedIn()) {
            console.warn("QuestionHub connection not found in global scope");
            ensureQuestionHubConnection();
        }
    }
    
    // Kiểm tra PresenceHub
    if (window.presenceConnection) {
        logConnectionStatus("PresenceHub", window.presenceConnection.state);
    } else {
        if (isUserLoggedIn()) {
            console.warn("PresenceHub connection not found in global scope");
            ensurePresenceHubConnection();
        }
    }
    
    // Kiểm tra ChatHub
    if (window.chatConnection) {
        logConnectionStatus("ChatHub", window.chatConnection.state);
    } else {
        if (isUserLoggedIn()) {
            console.warn("ChatHub connection not found in global scope");
            ensureChatHubConnection();
        }
    }
    
    // Kiểm tra ActivityHub
    if (window.activityConnection) {
        logConnectionStatus("ActivityHub", window.activityConnection.state);
    } else {
        if (isUserLoggedIn()) {
            console.warn("ActivityHub connection not found in global scope");
            ensureActivityHubConnection();
        }
    }
    
    console.groupEnd();
    
    // Kiểm tra lại sau 30 giây
    setTimeout(checkAllConnections, 30000);
}

function logConnectionStatus(hubName, state) {
    switch (state) {
        case "Connected":
            console.log(`✅ ${hubName}: Connected`);
            break;
        case "Connecting":
            console.log(`⏳ ${hubName}: Connecting...`);
            break;
        case "Disconnected":
            console.error(`❌ ${hubName}: Disconnected`);
            break;
        case "Reconnecting":
            console.warn(`🔄 ${hubName}: Reconnecting...`);
            break;
        default:
            console.warn(`❓ ${hubName}: Unknown state (${state})`);
    }
}

// Thêm function để khởi động lại kết nối khi cần
window.restartSignalRConnection = function(connectionName) {
    let connection = null;
    
    switch(connectionName) {
        case "notification":
            connection = window.notificationConnection;
            break;
        case "viewCount":
            connection = window.viewCountConnection;
            break;
        case "question":
            connection = window.questionHubConnection;
            break;
        case "presence":
            connection = window.presenceConnection;
            break;
        case "chat":
            connection = window.chatConnection;
            break;
        case "activity":
            connection = window.activityConnection;
            break;
    }
    
    if (connection) {
        console.log(`Attempting to restart ${connectionName} connection...`);
        connection.stop().then(() => {
            console.log(`${connectionName} connection stopped, restarting...`);
            connection.start().then(() => {
                console.log(`${connectionName} connection restarted successfully`);
            }).catch(err => {
                console.error(`Error restarting ${connectionName} connection:`, err);
            });
        }).catch(err => {
            console.error(`Error stopping ${connectionName} connection:`, err);
            // Thử start mà không cần stop
            connection.start().then(() => {
                console.log(`${connectionName} connection started without stopping first`);
            }).catch(err => {
                console.error(`Failed to restart ${connectionName} connection:`, err);
            });
        });
    } else {
        console.error(`Connection ${connectionName} not found`);
    }
}; 

// Thêm hàm tiện ích để hiển thị thông báo toast nếu hàm của notification-service không tồn tại
window.showNotificationToast = window.showNotificationToast || function(notification) {
    if (typeof Toastify === 'undefined') {
        console.warn('Toastify library not loaded, using alert instead');
        alert(`Notification: ${notification.title || notification.message}`);
        return;
    }
    
    Toastify({
        text: notification.title || notification.message,
        duration: 5000,
        close: true,
        gravity: "top",
        position: "right",
        backgroundColor: "#4caf50",
        stopOnFocus: true
    }).showToast();
}; 

// Tạo và hiển thị SignalR debug panel cho developers
function createSignalRDebugPanel() {
    if (document.querySelector('.signalr-debug-panel')) return;
    
    // Kiểm tra thời gian ẩn trong localStorage
    const hideTimestamp = localStorage.getItem('signalRDebugPanelHideTime');
    const currentTime = new Date().getTime();
    const fiveHoursInMs = 5 * 60 * 60 * 1000; // 5 giờ tính bằng mili giây
    
    // Nếu chưa đủ 5 tiếng từ lần ẩn trước, không hiển thị panel
    const initiallyHidden = hideTimestamp && (currentTime - parseInt(hideTimestamp) < fiveHoursInMs);
    
    const debugPanel = document.createElement('div');
    debugPanel.className = 'signalr-debug-panel';
    if (initiallyHidden) {
        debugPanel.classList.add('hidden');
    }
    
    debugPanel.innerHTML = `
        <h5>SignalR Connections <button class="btn-sm btn-outline-secondary float-end" id="toggleDebugPanel">Hide</button></h5>
        <div class="connections-container">
            <div class="connection-row">
                <span class="hub-name">NotificationHub</span>
                <span class="connection-status" id="status-notification">Checking...</span>
            </div>
            <div class="connection-row">
                <span class="hub-name">ViewCountHub</span>
                <span class="connection-status" id="status-viewcount">Checking...</span>
            </div>
            <div class="connection-row">
                <span class="hub-name">QuestionHub</span>
                <span class="connection-status" id="status-question">Checking...</span>
            </div>
            <div class="connection-row">
                <span class="hub-name">PresenceHub</span>
                <span class="connection-status" id="status-presence">Checking...</span>
            </div>
            <div class="connection-row">
                <span class="hub-name">ChatHub</span>
                <span class="connection-status" id="status-chat">Checking...</span>
            </div>
            <div class="connection-row">
                <span class="hub-name">ActivityHub</span>
                <span class="connection-status" id="status-activity">Checking...</span>
            </div>
        </div>
        <div class="debug-actions mt-2">
            <button class="btn-sm btn-primary" id="checkConnections">Check Now</button>
            <button class="btn-sm btn-danger" id="restartAllConnections">Restart All</button>
        </div>
    `;
    
    document.body.appendChild(debugPanel);
    
    // Xử lý sự kiện
    document.getElementById('toggleDebugPanel').addEventListener('click', function() {
        debugPanel.classList.add('hidden');
        this.textContent = 'Show';
        
        // Lưu thời điểm ẩn vào localStorage
        localStorage.setItem('signalRDebugPanelHideTime', new Date().getTime().toString());
        
        // Hiển thị thông báo về thời gian hiện lại
        if (window.showNotificationToast) {
            window.showNotificationToast({
                title: "SignalR Connections Panel Hidden",
                message: "Panel will reappear after 5 hours."
            });
        }
    });
    
    document.getElementById('checkConnections').addEventListener('click', function() {
        checkAllConnections();
        updateDebugPanel();
    });
    
    document.getElementById('restartAllConnections').addEventListener('click', function() {
        if (window.notificationConnection) window.restartSignalRConnection('notification');
        if (window.viewCountConnection) window.restartSignalRConnection('viewCount');
        if (window.questionHubConnection) window.restartSignalRConnection('question');
        if (window.presenceConnection) window.restartSignalRConnection('presence');
        if (window.chatConnection) window.restartSignalRConnection('chat');
        if (window.activityConnection) window.restartSignalRConnection('activity');
        
        setTimeout(updateDebugPanel, 1000);
    });
    
    function updateDebugPanel() {
        updateDebugStatus('notification', window.notificationConnection);
        updateDebugStatus('viewcount', window.viewCountConnection);
        updateDebugStatus('question', window.questionHubConnection);
        updateDebugStatus('presence', window.presenceConnection);
        updateDebugStatus('chat', window.chatConnection);
        updateDebugStatus('activity', window.activityConnection);
    }
    
    function updateDebugStatus(id, connection) {
        const statusEl = document.getElementById(`status-${id}`);
        if (!statusEl) return;
        
        statusEl.className = 'connection-status';
        
        if (!connection) {
            statusEl.textContent = 'Not found';
            statusEl.classList.add('status-disconnected');
            return;
        }
        
        switch(connection.state) {
            case 'Connected':
                statusEl.textContent = 'Connected';
                statusEl.classList.add('status-connected');
                break;
            case 'Connecting':
            case 'Reconnecting':
                statusEl.textContent = connection.state;
                statusEl.classList.add('status-connecting');
                break;
            case 'Disconnected':
                statusEl.textContent = 'Disconnected';
                statusEl.classList.add('status-disconnected');
                break;
            default:
                statusEl.textContent = connection.state || 'Unknown';
                statusEl.classList.add('status-disconnected');
        }
    }
    
    // Khởi tạo panel
    updateDebugPanel();
    setInterval(updateDebugPanel, 2000);
}

// Chỉ tạo debug panel trong môi trường development và kiểm tra thời gian ẩn
if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
    document.addEventListener('DOMContentLoaded', function() {
        // Kiểm tra xem đã đủ 5 tiếng kể từ lần ẩn gần nhất chưa
        const hideTimestamp = localStorage.getItem('signalRDebugPanelHideTime');
        const currentTime = new Date().getTime();
        const fiveHoursInMs = 5 * 60 * 60 * 1000; // 5 giờ tính bằng mili giây
        
        // Nếu đã đủ 5 tiếng hoặc chưa từng ẩn, tạo panel
        if (!hideTimestamp || (currentTime - parseInt(hideTimestamp) >= fiveHoursInMs)) {
            createSignalRDebugPanel();
            createDebugPanelToggle();
        } else {
            // Tạo một nút nhỏ để người dùng có thể hiển thị lại panel nếu muốn
            createDebugPanelToggle();
        }
    });
}

// Cập nhật hàm toggle để tính đến trạng thái ẩn trong 5 tiếng
function createDebugPanelToggle() {
    // Kiểm tra nếu nút đã tồn tại
    if (document.querySelector('.signalr-debug-panel-toggle')) return;
    
    const toggleBtn = document.createElement('div');
    toggleBtn.className = 'signalr-debug-panel-toggle';
    toggleBtn.innerHTML = '<i class="bi bi-hdd-network"></i>';
    toggleBtn.title = 'Toggle SignalR Debug Panel';
    
    document.body.appendChild(toggleBtn);
    
    toggleBtn.addEventListener('click', function() {
        let debugPanel = document.querySelector('.signalr-debug-panel');
        
        if (!debugPanel) {
            // Nếu panel chưa tồn tại, tạo mới nó
            createSignalRDebugPanel();
            debugPanel = document.querySelector('.signalr-debug-panel');
            // Xóa timestamp để panel hiển thị bình thường
            localStorage.removeItem('signalRDebugPanelHideTime');
        }
        
        if (debugPanel) {
            const wasHidden = debugPanel.classList.contains('hidden');
            debugPanel.classList.toggle('hidden');
            
            // Nếu đang ẩn panel, lưu thời điểm
            if (!wasHidden) { // Nếu trước đó không ẩn, tức là đang ẩn
                localStorage.setItem('signalRDebugPanelHideTime', new Date().getTime().toString());
            } else { // Nếu trước đó ẩn, tức là đang hiện
                localStorage.removeItem('signalRDebugPanelHideTime');
            }
            
            this.innerHTML = debugPanel.classList.contains('hidden') ? 
                '<i class="bi bi-hdd-network"></i>' : 
                '<i class="bi bi-x-lg"></i>';
        }
    });
} 