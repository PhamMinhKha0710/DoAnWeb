/**
 * Notification Service - Fixed
 * Dịch vụ xử lý tất cả các loại thông báo realtime
 * - Vote notifications
 * - Answer notifications
 * - Comment notifications
 * - Accept answer notifications
 */

// Biến global để lưu trữ trạng thái
let isNotiConnectionReady = false;

// Danh sách các loại thông báo được hỗ trợ
const NotificationTypes = {
    VOTE: 'vote',
    ANSWER: 'answer',
    COMMENT: 'comment',
    ACCEPT: 'accept',
    SYSTEM: 'system'
};

// Khởi tạo khi document sẵn sàng
document.addEventListener('DOMContentLoaded', function() {
    console.log("Notification Service: Initializing...");
    
    // Sử dụng SignalRLoader để đảm bảo SignalR được tải trước khi khởi tạo kết nối
    if (window.SignalRLoader) {
        SignalRLoader.ready(function() {
            console.log("SignalR loaded successfully, initializing notification service");
            // Kiểm tra đăng nhập trước khi khởi tạo
            if (isUserLoggedIn()) {
                initNotificationService();
            } else {
                console.log("Notification Service: User not logged in. Notifications require authentication.");
            }
        });
    } else {
        console.error("SignalRLoader not found. Make sure signalr-loader.js is loaded first.");
    }
});

/**
 * Kiểm tra xem người dùng đã đăng nhập hay chưa
 */
function isUserLoggedIn() {
    return document.querySelector('form#logoutForm') !== null ||
           document.querySelector('.nav-item.dropdown:has(.dropdown-item[onclick*="logoutForm"])') !== null ||
           document.querySelector('.dropdown-item[onclick*="logoutForm"]') !== null;
}

/**
 * Khởi tạo dịch vụ thông báo
 */
function initNotificationService() {
    console.log("Initializing notification service");
    
    // Kiểm tra kết nối đã tồn tại
    if (window.notificationConnection) {
        console.log("Using existing notification connection");
        
        // Cấu hình các handler nếu chưa có
        if (!window.notificationConnection._notificationListenersSetup) {
            setupNotificationListeners();
        }
        
        // Đảm bảo kết nối được khởi động
        if (window.notificationConnection.state === 'Disconnected') {
            startHubConnection(window.notificationConnection, 'NotificationHub');
        }
        
        return;
    }
    
    // Tạo kết nối mới nếu chưa có
    createNotificationConnection();
}

/**
 * Tạo kết nối SignalR cho NotificationHub
 */
function createNotificationConnection() {
    if (window.notificationConnection) {
        console.log("Notification connection already exists");
        return window.notificationConnection;
    }

    console.log("Creating notification connection");
    
    // Tạo kết nối mới
    window.notificationConnection = new signalR.HubConnectionBuilder()
        .withUrl("/notificationHub")
        .withAutomaticReconnect([0, 2000, 5000, 10000, 15000, 30000]) // Thử kết nối lại theo thời gian
        .configureLogging(signalR.LogLevel.Information)
        .build();
    
    // Khi kết nối thành công
    window.notificationConnection.onreconnected(connectionId => {
        console.log("Reconnected to notification hub with ID: " + connectionId);
        setupNotificationListeners();
    });
    
    // Thiết lập listener ngay sau khi tạo kết nối
    setupNotificationListeners();
    
    // Khởi động kết nối
    startHubConnection(window.notificationConnection, 'NotificationHub');
    
    return window.notificationConnection;
}

/**
 * Thiết lập các listener cho thông báo
 */
function setupNotificationListeners() {
    console.log("Setting up notification listeners");
    
    // Đánh dấu đã thiết lập để không thiết lập nhiều lần
    if (window.notificationConnection._notificationListenersSetup) {
        console.log("Notification listeners already set up");
        return;
    }

    // Listener cho thông báo vote
    window.notificationConnection.on('ReceiveVoteNotification', (notification) => {
        handleVoteNotification(notification);
    });

    // Listener cho thông báo answer
    window.notificationConnection.on('ReceiveAnswerNotification', (notification) => {
        handleAnswerNotification(notification);
    });

    // Listener cho thông báo comment
    window.notificationConnection.on('ReceiveCommentNotification', (notification) => {
        handleCommentNotification(notification);
    });

    // Listener cho thông báo accept
    window.notificationConnection.on('ReceiveAcceptNotification', (notification) => {
        handleAcceptNotification(notification);
    });

    // Listener cho thông báo mention
    window.notificationConnection.on('ReceiveMentionNotification', (notification) => {
        handleMentionNotification(notification);
    });
    
    // Thiết lập nút "Đánh dấu tất cả là đã đọc"
    setupMarkAllReadButton();
    
    // Đánh dấu đã thiết lập listener
    window.notificationConnection._notificationListenersSetup = true;
    console.log("Notification listeners set up successfully");
}

/**
 * Xử lý thông báo vote
 * @param {Object} notification - Thông báo vote
 */
function handleVoteNotification(notification) {
    console.log("Handling vote notification:", notification);
    
    // Tạo toast notification
    createToast({
        title: 'New Vote',
        message: notification.message,
        type: 'info'
    });
    
    // Thêm vào dropdown
    addToNotificationDropdown(notification);
    
    // Cập nhật badge
    updateNotificationBadge();
}

/**
 * Xử lý thông báo answer
 * @param {Object} notification - Thông báo câu trả lời
 */
function handleAnswerNotification(notification) {
    console.log("Handling answer notification:", notification);
    
    // Tạo toast notification
    createToast({
        title: 'New Answer',
        message: notification.message,
        type: 'info'
    });
    
    // Thêm vào dropdown
    addToNotificationDropdown(notification);
    
    // Cập nhật badge
    updateNotificationBadge();
}

/**
 * Xử lý thông báo comment
 * @param {Object} notification - Thông báo bình luận
 */
function handleCommentNotification(notification) {
    console.log("Handling comment notification:", notification);
    
    // Tạo toast notification
    createToast({
        title: 'New Comment',
        message: notification.message,
        type: 'info'
    });
    
    // Thêm vào dropdown
    addToNotificationDropdown(notification);
    
    // Cập nhật badge
    updateNotificationBadge();
}

/**
 * Xử lý thông báo accept answer
 * @param {Object} notification - Thông báo chấp nhận câu trả lời
 */
function handleAcceptNotification(notification) {
    console.log("Handling accept notification:", notification);
    
    // Tạo toast notification
    createToast({
        title: 'Answer Accepted',
        message: notification.message,
        type: 'success'
    });
    
    // Thêm vào dropdown
    addToNotificationDropdown(notification);
    
    // Cập nhật badge
    updateNotificationBadge();
}

/**
 * Xử lý thông báo mention
 * @param {Object} notification - Thông báo mention
 */
function handleMentionNotification(notification) {
    console.log("Handling mention notification:", notification);
    
    // Tạo toast notification
    createToast({
        title: 'You were mentioned',
        message: notification.message,
        type: 'info'
    });
    
    // Thêm vào dropdown
    addToNotificationDropdown(notification);
    
    // Cập nhật badge
    updateNotificationBadge();
}

/**
 * Xử lý các loại thông báo chung khác
 */
function handleGenericNotification(notification) {
    window.showNotificationToast(
        notification.title || 'Notification',
        notification.message,
        'bi-bell-fill text-secondary',
        'bg-secondary-subtle text-secondary',
        notification.url || getUrlFromNotification(notification)
    );
}

/**
 * Tạo và hiển thị thông báo toast
 * @param {Object} options - Các tùy chọn cho toast
 * @param {string} options.title - Tiêu đề thông báo
 * @param {string} options.message - Nội dung thông báo
 * @param {string} options.type - Loại thông báo (success, info, warning, error)
 */
function createToast(options) {
    try {
        console.log("Creating toast notification:", options);
        
        if (!options || !options.message) {
            console.error("Cannot create toast: missing required parameters");
            return;
        }
        
        const title = options.title || "Notification";
        const message = options.message;
        const type = options.type || "info";
        
        // Tạo toast sử dụng toastr nếu có
        if (typeof toastr !== 'undefined') {
            toastr.options = {
                closeButton: true,
                progressBar: true,
                positionClass: "toast-top-right",
                timeOut: 5000
            };
            
            switch (type) {
                case 'success':
                    toastr.success(message, title);
                    break;
                case 'warning':
                    toastr.warning(message, title);
                    break;
                case 'error':
                    toastr.error(message, title);
                    break;
                case 'info':
                default:
                    toastr.info(message, title);
                    break;
            }
        } else {
            // Tạo toast thủ công nếu không có toastr
            console.log(`${title}: ${message} (${type})`);
            
            // Tạo toast element
            const toast = document.createElement('div');
            toast.className = `custom-toast toast-${type}`;
            toast.innerHTML = `
                <div class="toast-header">
                    <strong>${title}</strong>
                    <button type="button" class="close" onclick="this.parentElement.parentElement.remove();">×</button>
                </div>
                <div class="toast-body">${message}</div>
            `;
            
            // Thêm vào container hoặc body
            const container = document.getElementById('toast-container') || document.body;
            container.appendChild(toast);
            
            // Tự động xóa sau 5 giây
            setTimeout(() => {
                if (toast && toast.parentElement) {
                    toast.parentElement.removeChild(toast);
                }
            }, 5000);
        }
    } catch (error) {
        console.error("Error creating toast notification:", error);
    }
}

/**
 * Cập nhật UI vote nếu đang ở trang liên quan
 */
function updateVoteUI(notification) {
    // Chỉ tiếp tục nếu có đầy đủ thông tin
    if (!notification || (!notification.questionId && !notification.answerId)) {
        return;
    }
    
    // Xác định loại và ID của item
    let itemType = notification.answerId ? 'answer' : 'question';
    let itemId = notification.answerId || notification.questionId;
    
    // Tìm phần tử score cần cập nhật
    const scoreElements = document.querySelectorAll(`.${itemType}-score[data-id="${itemId}"], .vote-count[data-id="${itemId}"][data-type="${itemType}"]`);
    if (scoreElements.length === 0) {
        console.log(`No score elements found for ${itemType} ${itemId}`);
        return; // Không ở trang chứa item này
    }
    
    // Cập nhật số điểm
    if (notification.score !== undefined) {
        scoreElements.forEach(function(element) {
            element.textContent = notification.score;
            
            // Thêm hiệu ứng highlight
            element.classList.add('score-updated');
            setTimeout(function() {
                element.classList.remove('score-updated');
            }, 2000);
        });
        
        console.log(`Updated score for ${itemType} ${itemId} to ${notification.score}`);
    }
}

/**
 * Thêm thông báo vào dropdown notification trong layout
 */
function addToNotificationDropdown(notification) {
    // Tìm dropdown notification trong layout
    const dropdown = document.querySelector('.notification-dropdown .notification-list');
    if (!dropdown) {
        console.warn("Notification dropdown not found in layout");
        return; // Không có dropdown
    }
    
    // Kiểm tra xem có thông báo "No notifications yet" không
    const emptyNotificationDiv = dropdown.querySelector('.text-center.py-4');
    if (emptyNotificationDiv) {
        // Xóa thông báo "No notifications yet" khi có thông báo mới
        emptyNotificationDiv.remove();
    }
    
    // Xác định icon dựa trên loại thông báo
    let iconClass = '';
    const notificationType = (notification.type || notification.notificationType || "").toLowerCase();
    
    if (notificationType === 'vote') {
        iconClass = 'bi-arrow-up-circle text-danger';
    } else if (notificationType === 'answer') {
        iconClass = 'bi-chat-dots text-success';
    } else if (notificationType === 'comment') {
        iconClass = 'bi-chat-left text-primary';
    } else if (notificationType === 'accept') {
        iconClass = 'bi-check-circle text-success';
    } else if (notificationType === 'mention') {
        iconClass = 'bi-at text-warning';
    } else {
        iconClass = 'bi-bell text-secondary';
    }
    
    // Tạo phần tử thông báo mới
    const notificationItem = document.createElement('a');
    notificationItem.href = notification.url || getUrlFromNotification(notification);
    notificationItem.className = 'notification-item unread text-decoration-none text-reset';
    
    // Tạo nội dung thông báo
    notificationItem.innerHTML = `
        <div class="d-flex align-items-start">
            <div class="notification-icon">
                <i class="${iconClass}"></i>
            </div>
            <div class="flex-grow-1">
                <div class="notification-title">${notification.title || notificationType.charAt(0).toUpperCase() + notificationType.slice(1)}</div>
                <p class="mb-0 small">${notification.message}</p>
                <small class="notification-time">${timeAgo(new Date())}</small>
            </div>
        </div>
    `;
    
    // Thêm vào đầu danh sách
    if (dropdown.firstChild) {
        dropdown.insertBefore(notificationItem, dropdown.firstChild);
    } else {
        dropdown.appendChild(notificationItem);
    }
    
    // Cập nhật badge số lượng thông báo
    updateNotificationBadge();
}

/**
 * Cập nhật số lượng thông báo trên badge
 */
function updateNotificationBadge() {
    const badge = document.querySelector('.notification-badge');
    if (!badge) {
        return;
    }
    
    // Lấy và tăng số lượng
    let count = parseInt(badge.textContent || '0');
    badge.textContent = count + 1;
    
    // Đảm bảo badge hiển thị
    badge.classList.remove('d-none');
}

/**
 * Phát âm thanh thông báo
 */
function playNotificationSound() {
    // Kiểm tra xem người dùng đã tắt âm thanh không
    const soundEnabled = localStorage.getItem('notification-sound-enabled') !== 'false';
    if (!soundEnabled) {
        return;
    }
    
    // Tạo và phát âm thanh thông báo
    const audio = new Audio('/sounds/notification.mp3');
    audio.volume = 0.5;
    audio.play().catch(err => {
        console.log("Could not play notification sound:", err);
    });
}

/**
 * Lấy URL từ thông báo nếu không có sẵn
 */
function getUrlFromNotification(notification) {
    if (notification.url) {
        return notification.url;
    }
    
    if (notification.questionId) {
        let url = `/Questions/Details/${notification.questionId}`;
        if (notification.answerId) {
            url += `#answer-${notification.answerId}`;
        }
        return url;
    }
    
    return '#';
}

/**
 * Helper function để hiển thị thời gian tương đối
 */
function timeAgo(date) {
    const seconds = Math.floor((new Date() - date) / 1000);
    
    let interval = seconds / 31536000;
    if (interval > 1) return Math.floor(interval) + " years ago";
    
    interval = seconds / 2592000;
    if (interval > 1) return Math.floor(interval) + " months ago";
    
    interval = seconds / 86400;
    if (interval > 1) return Math.floor(interval) + " days ago";
    
    interval = seconds / 3600;
    if (interval > 1) return Math.floor(interval) + " hours ago";
    
    interval = seconds / 60;
    if (interval > 1) return Math.floor(interval) + " minutes ago";
    
    if (seconds < 10) return "just now";
    
    return Math.floor(seconds) + " seconds ago";
}

/**
 * Thiết lập sự kiện cho nút "Đánh dấu tất cả là đã đọc"
 */
function setupMarkAllReadButton() {
    try {
        console.log("Setting up mark all read button");
        
        // Tìm nút "Mark all as read" trong dropdown thông báo
        const markAllReadBtn = document.querySelector('.mark-all-read');
        if (!markAllReadBtn) {
            console.log("Mark all read button not found in the DOM");
            return;
        }
        
        // Loại bỏ tất cả event listeners cũ (nếu có)
        const newButton = markAllReadBtn.cloneNode(true);
        markAllReadBtn.parentNode.replaceChild(newButton, markAllReadBtn);
        
        // Thêm event listener mới
        newButton.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            console.log("Mark all as read button clicked");
            
            // Gọi hàm xử lý đánh dấu tất cả là đã đọc
            markAllNotificationsAsRead();
        });
        
        console.log("Mark all read button setup completed");
    } catch (error) {
        console.error("Error setting up mark all read button:", error);
    }
}

/**
 * Đánh dấu tất cả thông báo là đã đọc
 */
function markAllNotificationsAsRead() {
    // Đảm bảo kết nối đã sẵn sàng
    if (!window.notificationConnection || window.notificationConnection.state !== "Connected") {
        console.warn("Notification connection not ready, cannot mark notifications as read");
        createToast({
            title: 'Connection Error',
            message: 'Cannot mark notifications as read. Connection not ready.',
            type: 'error'
        });
        return;
    }
    
    // Gọi phương thức trên server để đánh dấu tất cả là đã đọc
    try {
        console.log("Invoking MarkAllAsRead on server...");
        window.notificationConnection.invoke("MarkAllAsRead")
            .then(function() {
                console.log("All notifications marked as read successfully");
                
                // Cập nhật UI
                updateUIAfterMarkAllRead();
                
                // Hiển thị thông báo thành công
                createToast({
                    title: 'Success',
                    message: 'All notifications have been marked as read',
                    type: 'success'
                });
            })
            .catch(function(err) {
                console.error("Error marking all notifications as read:", err);
                createToast({
                    title: 'Error',
                    message: 'Failed to mark all notifications as read',
                    type: 'error'
                });
            });
    } catch (e) {
        console.error("Exception when marking all notifications as read:", e);
        createToast({
            title: 'Error',
            message: 'An error occurred while processing your request',
            type: 'error'
        });
    }
}

/**
 * Cập nhật UI sau khi đánh dấu tất cả thông báo là đã đọc
 */
function updateUIAfterMarkAllRead() {
    try {
        console.log("Updating UI after marking all notifications as read");
        
        // Bỏ class 'unread' khỏi tất cả items thông báo
        const notificationItems = document.querySelectorAll('.notification-item.unread');
        notificationItems.forEach(item => {
            item.classList.remove('unread');
            console.log(`Removed 'unread' class from item ${item.getAttribute('data-notification-id')}`);
        });
        
        // Cập nhật badge thông báo (đặt về 0)
        const badgeElement = document.querySelector('.notification-badge');
        if (badgeElement) {
            badgeElement.textContent = "0";
            badgeElement.style.display = 'none';
            console.log("Reset notification badge count to 0");
        }
        
        // Ẩn toast thông báo liên quan đến thông báo mới (nếu có)
        const toasts = document.querySelectorAll('.toast');
        toasts.forEach(toast => {
            if (toast.innerText.includes('notification')) {
                const bsToast = bootstrap.Toast.getInstance(toast);
                if (bsToast) {
                    bsToast.hide();
                }
            }
        });
    } catch (error) {
        console.error("Error updating UI after marking all notifications as read:", error);
    }
}

/**
 * Khởi động kết nối SignalR hub
 * @param {signalR.HubConnection} connection - Kết nối SignalR
 * @param {string} hubName - Tên của hub
 */
function startHubConnection(connection, hubName) {
    if (!connection) {
        console.error(`Cannot start ${hubName} connection: connection is undefined`);
        return Promise.reject(new Error(`Connection is undefined for ${hubName}`));
    }

    console.log(`Starting ${hubName} connection...`);
    
    return connection.start()
        .then(() => {
            console.log(`Successfully connected to ${hubName}`);
            return connection;
        })
        .catch(err => {
            console.error(`Error connecting to ${hubName}:`, err);
            
            // Thử kết nối lại sau 5 giây
            return new Promise((resolve) => {
                console.log(`Will retry ${hubName} connection in 5 seconds...`);
                setTimeout(() => {
                    startHubConnection(connection, hubName)
                        .then(resolve)
                        .catch(() => {
                            console.error(`Failed to reconnect to ${hubName} after retry`);
                            resolve(null);
                        });
                }, 5000);
            });
        });
}