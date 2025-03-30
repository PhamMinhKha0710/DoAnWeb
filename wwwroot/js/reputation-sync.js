/**
 * Reputation Sync - Đồng bộ điểm reputation theo thời gian thực
 * 
 * Tính năng:
 * - Kết nối và lắng nghe các thay đổi reputation từ NotificationHub
 * - Cập nhật tự động điểm reputation trên tất cả các trang
 * - Hiển thị animation khi điểm reputation thay đổi
 */

document.addEventListener('DOMContentLoaded', function() {
    console.log("Reputation Sync: Initializing...");
    
    // Sử dụng SignalRLoader để đảm bảo SignalR được tải trước khi thiết lập kết nối
    if (window.SignalRLoader) {
        SignalRLoader.ready(function() {
            console.log("SignalR loaded successfully, initializing reputation sync");
            initReputationSync();
        });
    } else {
        console.error("SignalRLoader not found. Make sure signalr-loader.js is loaded first.");
    }
});

/**
 * Khởi tạo kết nối và đồng bộ hóa reputation
 */
function initReputationSync() {
    // Kiểm tra xem người dùng đã đăng nhập chưa
    if (!isUserLoggedIn()) {
        console.log("User not logged in, reputation sync is disabled");
        return;
    }
    
    // Sử dụng kết nối notification hiện có nếu có
    if (window.notificationConnection) {
        setupReputationListeners(window.notificationConnection);
    } else if (window.hubConnections && window.hubConnections.notificationHub) {
        setupReputationListeners(window.hubConnections.notificationHub);
    } else {
        console.log("Creating new connection for reputation sync");
        createReputationConnection();
    }
}

/**
 * Kiểm tra đăng nhập bằng cách tìm form đăng xuất
 */
function isUserLoggedIn() {
    return document.querySelector('form#logoutForm') !== null ||
           document.querySelector('.dropdown-item[onclick*="logoutForm"]') !== null;
}

/**
 * Tạo kết nối đến NotificationHub
 */
function createReputationConnection() {
    if (window.SignalRLoader && window.SignalRLoader.getSignalR()) {
        const signalR = window.SignalRLoader.getSignalR();
        
        // Tạo kết nối đến NotificationHub
        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/notificationHub")
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Information)
            .build();
            
        // Thiết lập các listener
        setupReputationListeners(connection);
        
        // Bắt đầu kết nối
        startHubConnection(connection, "NotificationHub");
    } else {
        console.error("SignalR library not available");
    }
}

/**
 * Thiết lập các listener cho sự kiện reputation
 */
function setupReputationListeners(connection) {
    if (!connection) {
        console.error("Cannot setup reputation listeners - no connection");
        return;
    }
    
    // Nếu đã thiết lập listener, không thiết lập lại
    if (connection._reputationListenersSetup) {
        console.log("Reputation listeners already set up");
        return;
    }
    
    // Đăng ký listener cho sự kiện ReputationChanged
    connection.on("ReputationChanged", function(userId, newReputation, reason) {
        console.log("Reputation changed:", { userId, newReputation, reason });
        updateReputationInUI(userId, newReputation, reason);
    });
    
    // Đánh dấu đã thiết lập listener
    connection._reputationListenersSetup = true;
    console.log("Reputation listeners set up successfully");
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

    console.log(`Starting ${hubName} connection for reputation sync...`);
    
    return connection.start()
        .then(() => {
            console.log(`Successfully connected to ${hubName} for reputation sync`);
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

/**
 * Cập nhật điểm reputation trong giao diện người dùng
 * @param {number} userId - ID của người dùng
 * @param {number} newReputation - Giá trị reputation mới
 * @param {string} reason - Lý do thay đổi
 */
function updateReputationInUI(userId, newReputation, reason) {
    // Kiểm tra nếu đây là người dùng hiện tại (trang profile cá nhân)
    const currentUserElements = document.querySelectorAll('.user-reputation[data-user-id="' + userId + '"]');
    
    if (currentUserElements && currentUserElements.length > 0) {
        currentUserElements.forEach(el => {
            // Lưu giá trị cũ để hiệu ứng
            const oldValue = parseInt(el.textContent);
            
            // Tạo hiệu ứng thay đổi số
            animateReputationChange(el, oldValue, newReputation);
            
            // Hiển thị lý do (tùy chọn)
            if (reason) {
                showReputationChangeToast(oldValue, newReputation, reason);
            }
        });
    }
    
    // Cập nhật trong danh sách người dùng (nếu có)
    updateReputationInUserList(userId, newReputation);
    
    // Cập nhật trong chi tiết người dùng (nếu có)
    updateReputationInUserDetails(userId, newReputation);
}

/**
 * Tạo hiệu ứng animation khi thay đổi điểm số
 * @param {HTMLElement} element - Phần tử hiển thị điểm
 * @param {number} oldValue - Giá trị cũ
 * @param {number} newValue - Giá trị mới
 */
function animateReputationChange(element, oldValue, newValue) {
    if (!element) return;
    
    // Clone phần tử hiện tại để tạo hiệu ứng overlay
    const overlay = document.createElement('div');
    overlay.style.position = 'absolute';
    overlay.style.top = '0';
    overlay.style.left = '0';
    overlay.style.width = '100%';
    overlay.style.height = '100%';
    overlay.style.display = 'flex';
    overlay.style.alignItems = 'center';
    overlay.style.justifyContent = 'center';
    overlay.style.zIndex = '1000';
    overlay.style.pointerEvents = 'none';

    // Hiển thị sự thay đổi bằng một hiệu ứng +/- xuất hiện và bay lên
    const change = newValue - oldValue;
    const indicator = document.createElement('div');
    indicator.textContent = (change > 0 ? '+' : '') + change;
    indicator.style.color = change > 0 ? '#28a745' : '#dc3545';
    indicator.style.fontWeight = 'bold';
    indicator.style.position = 'absolute';
    indicator.style.fontSize = '1.2em';
    indicator.style.opacity = '0';
    indicator.style.transform = 'translateY(0)';
    indicator.style.transition = 'transform 1.5s ease-out, opacity 1.5s ease-out';
    
    // Thêm vào DOM
    overlay.appendChild(indicator);
    element.style.position = 'relative';
    element.appendChild(overlay);
    
    // Khởi động animation
    setTimeout(() => {
        indicator.style.opacity = '1';
    }, 10);
    
    setTimeout(() => {
        indicator.style.transform = 'translateY(-20px)';
        indicator.style.opacity = '0';
    }, 20);
    
    // Cập nhật giá trị mới
    setTimeout(() => {
        element.textContent = newValue;
        element.classList.add('highlight-update');
        
        // Xóa overlay sau khi hoàn thành
        setTimeout(() => {
            element.removeChild(overlay);
            element.classList.remove('highlight-update');
        }, 1500);
    }, 500);
}

/**
 * Cập nhật điểm trong danh sách người dùng
 * @param {number} userId - ID của người dùng
 * @param {number} newReputation - Giá trị reputation mới
 */
function updateReputationInUserList(userId, newReputation) {
    // Tìm và cập nhật trong danh sách người dùng (trang User Index)
    const userCards = document.querySelectorAll('.user-card');
    
    userCards.forEach(card => {
        const userLink = card.querySelector('.user-name a');
        if (userLink && userLink.getAttribute('href').includes('/Users/Details/' + userId)) {
            const reputationElement = card.querySelector('.user-stat-value');
            if (reputationElement) {
                const oldValue = parseInt(reputationElement.textContent);
                animateReputationChange(reputationElement, oldValue, newReputation);
            }
        }
    });
}

/**
 * Cập nhật điểm trong trang chi tiết người dùng
 * @param {number} userId - ID của người dùng
 * @param {number} newReputation - Giá trị reputation mới
 */
function updateReputationInUserDetails(userId, newReputation) {
    // Tìm và cập nhật trong trang chi tiết người dùng
    const profileBadges = document.querySelectorAll('.user-profile-badge.highlight');
    
    profileBadges.forEach(badge => {
        if (badge.textContent.includes('reputation')) {
            // Trích xuất giá trị điểm hiện tại
            const text = badge.textContent;
            const oldValue = parseInt(text.match(/\d+/)[0]);
            
            // Tạo nội dung mới
            const newText = text.replace(oldValue, newReputation);
            
            // Animation
            const badgeClone = badge.cloneNode(true);
            badgeClone.style.position = 'absolute';
            badgeClone.style.zIndex = '1';
            badgeClone.style.width = badge.offsetWidth + 'px';
            
            badge.parentNode.insertBefore(badgeClone, badge);
            badge.style.opacity = '0';
            
            // Animate the clone out
            badgeClone.style.transition = 'transform 0.5s ease, opacity 0.5s ease';
            setTimeout(() => {
                badgeClone.style.transform = 'translateY(-10px)';
                badgeClone.style.opacity = '0';
                
                // Update the real badge
                badge.textContent = newText;
                badge.style.opacity = '1';
                
                // Remove the clone
                setTimeout(() => {
                    if (badgeClone.parentNode) {
                        badgeClone.parentNode.removeChild(badgeClone);
                    }
                }, 500);
            }, 10);
        }
    });
}

/**
 * Hiển thị thông báo toast về thay đổi điểm
 * @param {number} oldValue - Giá trị cũ
 * @param {number} newValue - Giá trị mới
 * @param {string} reason - Lý do thay đổi
 */
function showReputationChangeToast(oldValue, newValue, reason) {
    try {
        const change = newValue - oldValue;
        const title = change > 0 ? 'Reputation Increased' : 'Reputation Decreased';
        
        // Sử dụng hàm createToast nếu có
        if (typeof createToast === 'function') {
            createToast({
                title: title,
                message: `${Math.abs(change)} points ${change > 0 ? 'gained' : 'lost'} for ${reason}`,
                type: change > 0 ? 'success' : 'warning'
            });
        } 
        // Sử dụng toastr nếu có
        else if (typeof toastr !== 'undefined') {
            const message = `${Math.abs(change)} points ${change > 0 ? 'gained' : 'lost'} for ${reason}`;
            
            if (change > 0) {
                toastr.success(message, title);
            } else {
                toastr.warning(message, title);
            }
        }
        // Nếu không có gì khả dụng, tạo toast riêng
        else {
            console.log(`${title}: ${Math.abs(change)} points ${change > 0 ? 'gained' : 'lost'} for ${reason}`);
        }
    } catch (error) {
        console.error("Error showing reputation change toast:", error);
    }
} 