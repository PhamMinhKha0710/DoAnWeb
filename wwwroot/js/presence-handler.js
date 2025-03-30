/**
 * Presence Handler - Quản lý theo dõi trạng thái online của người dùng
 */

// Khởi tạo khi trang đã load
document.addEventListener('DOMContentLoaded', function() {
    console.log("Presence Handler: Initializing...");

    // Sử dụng SignalRLoader để đảm bảo SignalR được tải trước khi thiết lập kết nối
    if (window.SignalRLoader) {
        SignalRLoader.ready(function() {
            console.log("SignalR loaded successfully, initializing presence handler");
            initPresenceHandler();
        });
    } else {
        console.error("SignalRLoader not found. Make sure signalr-loader.js is loaded first.");
    }
});

/**
 * Khởi tạo Presence Handler
 */
function initPresenceHandler() {
    // Chỉ khởi tạo handler nếu người dùng đã đăng nhập
    if (!isUserLoggedIn()) {
        console.log("Presence Handler: User not logged in, skipping");
        return;
    }
    
    // Kiểm tra kết nối đã tồn tại
    if (window.presenceConnection) {
        console.log("Presence Handler: Using existing PresenceHub connection");
        setupPresenceHandlers(window.presenceConnection);
    } else {
        // Nếu signalr-connection-check.js được tải, nó sẽ tạo kết nối
        console.log("Presence Handler: Waiting for PresenceHub connection to be created");
        
        // Đợi kết nối được tạo
        const checkInterval = setInterval(function() {
            if (window.presenceConnection) {
                console.log("Presence Handler: PresenceHub connection found");
                clearInterval(checkInterval);
                setupPresenceHandlers(window.presenceConnection);
            }
        }, 1000);
        
        // Hủy interval sau 10 giây nếu không tìm thấy kết nối
        setTimeout(function() {
            clearInterval(checkInterval);
        }, 10000);
    }
}

/**
 * Kiểm tra người dùng đã đăng nhập chưa
 */
function isUserLoggedIn() {
    return document.querySelector('form#logoutForm') !== null || 
           document.querySelector('.dropdown-item[onclick*="logoutForm"]') !== null;
}

/**
 * Thiết lập handlers cho PresenceHub
 */
function setupPresenceHandlers(connection) {
    // Đảm bảo chỉ thiết lập một lần
    if (connection._presenceHandlersSetup) {
        return;
    }
    
    console.log("Presence Handler: Setting up PresenceHub handlers");
    
    // Xử lý sự kiện người dùng online
    connection.on("UserOnline", function(user) {
        console.log("User came online:", user);
        updateUserPresence(user.userId, true);
    });
    
    // Xử lý sự kiện người dùng offline
    connection.on("UserOffline", function(user) {
        console.log("User went offline:", user);
        updateUserPresence(user.userId, false);
    });
    
    // Xử lý danh sách người dùng online
    connection.on("OnlineUsers", function(userIds) {
        console.log("Online users list received:", userIds);
        updateOnlineUsersList(userIds);
    });
    
    // Xử lý số lượng người dùng online
    connection.on("OnlineCount", function(count) {
        console.log("Online users count:", count);
        updateOnlineCount(count);
    });
    
    // Đánh dấu đã thiết lập
    connection._presenceHandlersSetup = true;
    
    // Yêu cầu số lượng người dùng online hiện tại
    if (connection.state === "Connected") {
        getOnlineCount(connection);
    }
}

/**
 * Yêu cầu số lượng người dùng online
 */
function getOnlineCount(connection) {
    if (connection && connection.state === "Connected") {
        connection.invoke("GetOnlineCount")
            .catch(function(err) {
                console.error("Error getting online count:", err);
            });
    }
}

/**
 * Cập nhật trạng thái hiển thị của người dùng
 */
function updateUserPresence(userId, isOnline) {
    const userElements = document.querySelectorAll(`[data-user-id="${userId}"]`);
    
    userElements.forEach(function(element) {
        // Cập nhật class
        element.classList.remove(isOnline ? 'user-offline' : 'user-online');
        element.classList.add(isOnline ? 'user-online' : 'user-offline');
        
        // Cập nhật indicator nếu có
        const indicator = element.querySelector('.presence-indicator');
        if (indicator) {
            indicator.classList.remove('online', 'offline');
            indicator.classList.add(isOnline ? 'online' : 'offline');
            indicator.title = isOnline ? 'Online' : 'Offline';
        }
    });
}

/**
 * Cập nhật danh sách người dùng online
 */
function updateOnlineUsersList(userIds) {
    // Đặt tất cả người dùng về trạng thái offline trước
    const allUserElements = document.querySelectorAll('[data-user-id]');
    allUserElements.forEach(function(element) {
        element.classList.remove('user-online');
        element.classList.add('user-offline');
        
        const indicator = element.querySelector('.presence-indicator');
        if (indicator) {
            indicator.classList.remove('online');
            indicator.classList.add('offline');
            indicator.title = 'Offline';
        }
    });
    
    // Cập nhật người dùng online
    userIds.forEach(function(userId) {
        updateUserPresence(userId, true);
    });
}

/**
 * Cập nhật số lượng người dùng online
 */
function updateOnlineCount(count) {
    const countElements = document.querySelectorAll('.online-users-count');
    countElements.forEach(function(element) {
        element.textContent = count;
    });
} 