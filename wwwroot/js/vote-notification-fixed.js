/**
 * Vote Notification Handler - Fixed
 * Phiên bản cải tiến của vote notification handler
 */

// Biến global để lưu trữ kết nối
let voteNotificationConnection = null;

document.addEventListener('DOMContentLoaded', function() {
    console.log("Vote Notification Fixed: Initializing...");
    
    // Chỉ khởi tạo nếu người dùng đã đăng nhập
    const userIdentityElement = document.querySelector('[data-user-identity]');
    if (userIdentityElement) {
        initVoteNotifications();
    } else {
        console.log("User not logged in, skipping vote notification setup");
    }
});

/**
 * Khởi tạo xử lý thông báo vote
 */
function initVoteNotifications() {
    // Tìm kết nối từ các nguồn khác nhau
    if (window.notificationConnection) {
        voteNotificationConnection = window.notificationConnection;
        console.log("Using existing notification connection");
        setupVoteNotificationListener();
    } else {
        // Nếu không có sẵn, tạo kết nối mới
        createNotificationConnection();
    }
}

/**
 * Tạo kết nối mới cho NotificationHub
 */
function createNotificationConnection() {
    try {
        console.log("Creating new NotificationHub connection...");
        
        // Tạo kết nối SignalR
        voteNotificationConnection = new signalR.HubConnectionBuilder()
            .withUrl("/notificationHub")
            .withAutomaticReconnect([0, 2000, 5000, 10000, 20000])
            .configureLogging(signalR.LogLevel.Information)
            .build();
            
        // Đăng ký sự kiện kết nối/ngắt kết nối
        voteNotificationConnection.onreconnected(function() {
            console.log("NotificationHub reconnected, setting up vote listener");
            setupVoteNotificationListener();
        });
        
        // Bắt đầu kết nối
        voteNotificationConnection.start()
            .then(function() {
                console.log("NotificationHub connected successfully");
                setupVoteNotificationListener();
                
                // Lưu vào biến toàn cục để các module khác có thể sử dụng
                window.notificationConnection = voteNotificationConnection;
            })
            .catch(function(err) {
                console.error("Error connecting to NotificationHub:", err);
            });
    } catch (err) {
        console.error("Error creating NotificationHub connection:", err);
    }
}

/**
 * Thiết lập listener cho thông báo vote
 */
function setupVoteNotificationListener() {
    if (!voteNotificationConnection) {
        console.error("Cannot setup vote notification listener - no connection");
        return;
    }
    
    // Xóa listener cũ (nếu có) để tránh đăng ký nhiều lần
    voteNotificationConnection.off("ReceiveNotification");
    
    // Đăng ký handler mới
    voteNotificationConnection.on("ReceiveNotification", function(notification) {
        console.log("Received notification:", notification);
        
        // Kiểm tra xem đây có phải là thông báo vote không
        if (notification && (notification.type === "Vote" || notification.type === "vote")) {
            console.log("Processing vote notification");
            
            // Hiển thị toast notification
            showVoteNotification(notification);
            
            // Cập nhật UI nếu chúng ta đang ở trang liên quan
            updateVoteUI(notification);
            
            // Thêm vào dropdown notification (nếu có)
            addToNotificationDropdown(notification);
        }
    });
    
    console.log("Vote notification listener set up successfully");
}

/**
 * Hiển thị thông báo vote
 */
function showVoteNotification(notification) {
    // Tạo hoặc lấy container toast
    let toastContainer = document.getElementById('vote-toast-container');
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.id = 'vote-toast-container';
        toastContainer.className = 'position-fixed top-0 end-0 p-3';
        toastContainer.style.zIndex = '1080';
        document.body.appendChild(toastContainer);
    }
    
    // Tạo ID duy nhất cho toast này
    const toastId = 'vote-toast-' + Date.now();
    
    // Xác định biểu tượng dựa trên loại vote
    let icon = 'bi-hand-thumbs-up-fill text-primary';
    if (notification.voteType === 'down') {
        icon = 'bi-hand-thumbs-down-fill text-danger';
    }
    
    // Tạo HTML cho toast
    const toast = document.createElement('div');
    toast.id = toastId;
    toast.className = 'toast vote-notification-toast';
    toast.setAttribute('role', 'alert');
    toast.setAttribute('aria-live', 'assertive');
    toast.setAttribute('aria-atomic', 'true');
    toast.innerHTML = `
        <div class="toast-header">
            <i class="bi ${icon} me-2"></i>
            <strong class="me-auto">${notification.title || 'Vote Notification'}</strong>
            <small>${timeAgo(new Date(notification.createdDate || new Date()))}</small>
            <button type="button" class="btn-close" data-bs-dismiss="toast" aria-label="Close"></button>
        </div>
        <div class="toast-body">
            ${notification.message}
            ${notification.questionId ? 
                `<div class="mt-2 pt-2 border-top">
                    <a href="/Questions/Details/${notification.questionId}${notification.answerId ? '#answer-' + notification.answerId : ''}" 
                       class="btn btn-primary btn-sm">View</a>
                </div>` : 
                ''}
        </div>
    `;
    
    // Thêm vào container
    toastContainer.appendChild(toast);
    
    // Khởi tạo và hiển thị toast bằng Bootstrap
    const bsToast = new bootstrap.Toast(toast, {
        autohide: true,
        delay: 6000
    });
    bsToast.show();
    
    // Xóa toast sau khi đã ẩn để tránh tràn bộ nhớ
    toast.addEventListener('hidden.bs.toast', function() {
        toast.remove();
    });
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
    const scoreElements = document.querySelectorAll(`.${itemType}-score[data-id="${itemId}"]`);
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
 * Thêm thông báo vào dropdown notification
 */
function addToNotificationDropdown(notification) {
    // Tìm dropdown notification
    const dropdown = document.querySelector('.notification-dropdown-menu');
    if (!dropdown) {
        return; // Không có dropdown
    }
    
    // Tìm container các thông báo
    const notificationsList = dropdown.querySelector('.notifications-list');
    if (!notificationsList) {
        return;
    }
    
    // Tạo phần tử thông báo mới
    const notificationItem = document.createElement('a');
    notificationItem.href = notification.url || '#';
    notificationItem.className = 'dropdown-item notification-item unread';
    
    // Xác định icon dựa trên loại vote
    let iconClass = 'bi-hand-thumbs-up-fill text-primary';
    if (notification.voteType === 'down') {
        iconClass = 'bi-hand-thumbs-down-fill text-danger';
    }
    
    // Tạo nội dung thông báo
    notificationItem.innerHTML = `
        <div class="d-flex align-items-center">
            <div class="notification-icon me-3">
                <i class="bi ${iconClass}"></i>
            </div>
            <div class="flex-grow-1">
                <div class="fw-bold">${notification.title || 'Vote Notification'}</div>
                <div class="small">${notification.message}</div>
                <div class="text-muted smallest">${timeAgo(new Date(notification.createdDate || new Date()))}</div>
            </div>
        </div>
    `;
    
    // Thêm vào đầu danh sách
    if (notificationsList.firstChild) {
        notificationsList.insertBefore(notificationItem, notificationsList.firstChild);
    } else {
        notificationsList.appendChild(notificationItem);
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