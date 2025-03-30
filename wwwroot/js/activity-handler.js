/**
 * Activity Handler - Quản lý theo dõi hoạt động thông qua ActivityHub
 * Xử lý các phương thức client cần thiết để tránh warning
 */

// Khởi tạo khi trang đã load
document.addEventListener('DOMContentLoaded', function() {
    console.log("Activity Handler: Initializing...");

    // Sử dụng SignalRLoader để đảm bảo SignalR được tải trước khi thiết lập kết nối
    if (window.SignalRLoader) {
        SignalRLoader.ready(function() {
            console.log("SignalR loaded successfully, initializing activity handler");
            initActivityHandler();
        });
    } else {
        console.error("SignalRLoader not found. Make sure signalr-loader.js is loaded first.");
    }
});

/**
 * Khởi tạo Activity Handler
 */
function initActivityHandler() {
    // Chỉ khởi tạo handler nếu kết nối đã tồn tại
    if (window.activityConnection) {
        console.log("Activity Handler: Using existing ActivityHub connection");
        setupActivityHandlers(window.activityConnection);
    } else {
        // Nếu signalr-connection-check.js được tải, nó sẽ tạo kết nối
        console.log("Activity Handler: Waiting for ActivityHub connection to be created");
        
        // Đợi kết nối được tạo
        const checkInterval = setInterval(function() {
            if (window.activityConnection) {
                console.log("Activity Handler: ActivityHub connection found");
                clearInterval(checkInterval);
                setupActivityHandlers(window.activityConnection);
            }
        }, 1000);
        
        // Hủy interval sau 10 giây nếu không tìm thấy kết nối
        setTimeout(function() {
            clearInterval(checkInterval);
        }, 10000);
    }
}

/**
 * Thiết lập handlers cho ActivityHub
 */
function setupActivityHandlers(connection) {
    // Đảm bảo chỉ thiết lập một lần
    if (connection._activityHandlersSetup) {
        return;
    }
    
    console.log("Activity Handler: Setting up ActivityHub handlers");
    
    // Xử lý thống kê hoạt động - phương thức cần thiết để tránh warning
    connection.on("ActivityStats", function(stats) {
        console.log("Activity stats received:", stats);
        updateActivityStats(stats);
    });
    
    // Xử lý danh sách người dùng trực tuyến - phương thức cần thiết để tránh warning
    connection.on("OnlineUsers", function(users) {
        console.log("Online users list received:", users);
        updateOnlineUsers(users);
    });
    
    // Xử lý lượt xem trang mới
    connection.on("NewPageView", function(pageView) {
        console.log("New page view:", pageView);
    });
    
    // Xử lý hoạt động
    connection.on("ActivityAction", function(action) {
        console.log("Activity action:", action);
    });
    
    // Xử lý hoạt động chi tiết
    connection.on("DetailedActivity", function(activity) {
        console.log("Detailed activity:", activity);
    });
    
    // Đánh dấu đã thiết lập
    connection._activityHandlersSetup = true;
}

/**
 * Cập nhật thống kê hoạt động trên UI
 */
function updateActivityStats(stats) {
    // Cập nhật số lượng người dùng đang hoạt động
    const activeUsersElements = document.querySelectorAll('.active-users-count');
    activeUsersElements.forEach(function(element) {
        element.textContent = stats.activeConnections;
    });
    
    // Cập nhật tổng số lượt xem trang
    const pageViewsElements = document.querySelectorAll('.page-views-count');
    pageViewsElements.forEach(function(element) {
        element.textContent = stats.totalPageViews;
    });
}

/**
 * Cập nhật danh sách người dùng trực tuyến
 */
function updateOnlineUsers(users) {
    // Cập nhật số lượng người dùng trực tuyến
    const onlineUsersCountElements = document.querySelectorAll('.online-users-count');
    onlineUsersCountElements.forEach(function(element) {
        element.textContent = users.length;
    });
    
    // Cập nhật trạng thái trực tuyến cho từng người dùng
    users.forEach(function(userId) {
        const userElements = document.querySelectorAll(`[data-user-id="${userId}"]`);
        userElements.forEach(function(element) {
            element.classList.add('user-online');
            
            const statusIndicator = element.querySelector('.status-indicator');
            if (statusIndicator) {
                statusIndicator.classList.remove('offline');
                statusIndicator.classList.add('online');
            }
        });
    });
} 