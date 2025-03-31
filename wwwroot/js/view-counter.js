/**
 * View Counter
 * Client-side script để xử lý đếm lượt xem theo thời gian thực
 */

// Khởi tạo biến global
let questionId = null;
let isViewCounted = false;
let isConnectionReady = false;
let scrollListenerAdded = false;
let debugMode = true; // Enable debug mode

// Log function with timestamp
function logDebug(...args) {
    if (!debugMode) return;
    const timestamp = new Date().toISOString().split('T')[1].split('.')[0];
    console.log(`[${timestamp}] [ViewCounter]`, ...args);
}

document.addEventListener('DOMContentLoaded', function() {
    logDebug("ViewCounter: Initializing...");
    logDebug("Document ready - checking for question ID and SignalR availability");

    // Check if SignalR is loaded directly
    if (typeof signalR !== 'undefined') {
        logDebug("SignalR found directly, proceeding with initialization");
        initViewCounter();
    }
    // Fallback to SignalRLoader
    else if (window.SignalRLoader) {
        logDebug("SignalRLoader found, waiting for SignalR to be ready");
        SignalRLoader.ready(function() {
            logDebug("SignalR loaded successfully via loader, initializing view counter");
            initViewCounter();
        });
    } else {
        console.error("No SignalR found. View counter will not work.");
    }
});

// Khởi tạo View Counter
function initViewCounter() {
    // Nhận questionId từ meta tag
    const questionIdMeta = document.querySelector('meta[name="question-id"]');
    if (questionIdMeta) {
        questionId = parseInt(questionIdMeta.content);
        logDebug("Found question ID:", questionId);
        
        // Kiểm tra xem đã đếm view cho question này chưa (dùng localStorage)
        const viewedQuestions = JSON.parse(localStorage.getItem('viewedQuestions') || '{}');
        if (viewedQuestions[questionId]) {
            logDebug(`Question ${questionId} already viewed in this session, not counting again`);
            isViewCounted = true;
        }
        
        // Khởi tạo kết nối với ViewCountHub
        initializeViewCountHub();
        
        // Thiết lập scroll listener để tăng view khi cuộn đến giữa trang
        setupScrollListener();
        
        // Bắt đầu kiểm tra vị trí cuộn ngay khi trang tải
        checkScrollPosition();
    } else {
        logDebug("No question ID found, skipping view counter initialization");
    }
}

// Kiểm tra vị trí cuộn ngay khi trang tải
function checkScrollPosition() {
    // Đảm bảo không đếm lại nếu đã đếm
    if (isViewCounted) {
        logDebug("View already counted, not checking scroll position");
        return;
    }
    
    // Đợi DOM hoàn toàn tải xong
    setTimeout(() => {
        logDebug("Checking initial scroll position");
        
        // Kiểm tra xem nội dung có ngắn không (có thể thấy toàn bộ nội dung mà không cần cuộn)
        const scrollHeight = document.documentElement.scrollHeight;
        const clientHeight = document.documentElement.clientHeight;
        
        logDebug(`Document height: ${scrollHeight}px, Viewport height: ${clientHeight}px`);
        
        // Nếu nội dung ngắn hơn hoặc bằng viewport + 20% (cho phép hiển thị hầu hết nội dung)
        if (scrollHeight <= clientHeight * 1.2) {
            logDebug("Short content detected, counting view without scrolling");
            increaseViewCount();
        } else {
            logDebug("Content requires scrolling, waiting for scroll event");
        }
    }, 1000);
}

// Hàm thiết lập scroll listener
function setupScrollListener() {
    // Chỉ thêm listener một lần
    if (scrollListenerAdded || isViewCounted) {
        logDebug(`Skip setting up scroll listener: already added=${scrollListenerAdded}, already counted=${isViewCounted}`);
        return;
    }
    
    logDebug("Setting up scroll listener for view counting");
    
    // Biến để theo dõi xem đã đạt đến ngưỡng cuộn chưa
    let scrollThresholdReached = false;
    
    // Hàm xử lý sự kiện cuộn
    function handleScroll() {
        // Nếu đã đếm view rồi thì không cần làm gì nữa
        if (isViewCounted) {
            logDebug("View already counted, removing scroll listener");
            window.removeEventListener('scroll', handleScroll);
            return;
        }
        
        // Kiểm tra vị trí cuộn
        const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
        const scrollHeight = document.documentElement.scrollHeight;
        const clientHeight = document.documentElement.clientHeight;
        
        // Tính phần trăm đã cuộn
        const scrollPercent = (scrollTop / (scrollHeight - clientHeight)) * 100;
        
        // Thêm log để debug
        if (Math.floor(scrollPercent) % 10 === 0 && !scrollThresholdReached) {
            logDebug(`Current scroll position: ${scrollPercent.toFixed(2)}%`);
        }
        
        // Giảm ngưỡng xuống 30% để dễ kích hoạt hơn
        if (scrollPercent >= 30 && !scrollThresholdReached) {
            logDebug(`Scroll threshold reached (${scrollPercent.toFixed(2)}%), triggering view count`);
            scrollThresholdReached = true;
            
            // Tăng lượt xem
            increaseViewCount();
            
            // Sau khi đã tăng view, không cần lắng nghe sự kiện cuộn nữa
            window.removeEventListener('scroll', handleScroll);
        }
    }
    
    // Thêm sự kiện lắng nghe cuộn trang
    window.addEventListener('scroll', handleScroll, { passive: true });
    
    // Kích hoạt kiểm tra ngay lập tức để xem vị trí cuộn hiện tại
    handleScroll();
    
    // Đánh dấu đã thêm listener
    scrollListenerAdded = true;
    logDebug("Scroll listener added successfully");
}

// Thiết lập các event handler cho ViewCountHub
function setupViewCountHandlers() {
    // Đảm bảo chỉ thiết lập một lần
    if (window.viewCountConnection && !window.viewCountConnection._viewCountHandlersSetup) {
        logDebug("Setting up ViewCountHub event handlers");
        
        window.viewCountConnection.on("ReceiveUpdatedViewCount", function(qId, viewCount) {
            logDebug(`ViewCountHub: Received updated view count for question ${qId}: ${viewCount}`);
            if (qId === questionId) {
                updateViewCountDisplay(viewCount);
            }
        });
        
        window.viewCountConnection.on("ReceiveCurrentViewCount", function(qId, viewCount) {
            logDebug(`ViewCountHub: Received current view count for question ${qId}: ${viewCount}`);
            if (qId === questionId) {
                updateViewCountDisplay(viewCount);
            }
        });
        
        // Đánh dấu đã thiết lập handler
        window.viewCountConnection._viewCountHandlersSetup = true;
        logDebug("ViewCountHub event handlers set up successfully");
    }
}

// Yêu cầu số lượt xem hiện tại
function requestCurrentViewCount() {
    if (questionId && window.viewCountConnection && window.viewCountConnection.state === "Connected") {
        logDebug("Requesting current view count for question:", questionId);
        window.viewCountConnection.invoke("GetCurrentViewCount", questionId)
            .catch(err => {
                console.error("Error getting current view count:", err);
            });
    } else {
        logDebug("Cannot request view count - Connection ready:", 
                    window.viewCountConnection ? window.viewCountConnection.state : "no connection", 
                    "Question ID:", questionId);
    }
}

// Khởi tạo kết nối với ViewCountHub
function initializeViewCountHub() {
    try {
        logDebug("Creating ViewCountHub connection...");
        
        // Tạo kết nối SignalR
        window.viewCountConnection = new signalR.HubConnectionBuilder()
            .withUrl("/viewCountHub")
            .withAutomaticReconnect()
            .build();
         
        // Đăng ký event handlers
        setupViewCountHandlers();
        
        // Xử lý kết nối/ngắt kết nối
        window.viewCountConnection.onreconnected(function(connectionId) {
            logDebug(`ViewCountHub: Reconnected with ID ${connectionId}`);
            isConnectionReady = true;
            requestCurrentViewCount();
        });
        
        window.viewCountConnection.onclose(function(error) {
            logDebug("ViewCountHub: Connection closed", error);
            isConnectionReady = false;
        });
        
        // Bắt đầu kết nối
        logDebug("Starting ViewCountHub connection...");
        window.viewCountConnection.start()
            .then(function() {
                logDebug("ViewCountHub: Connected successfully");
                isConnectionReady = true;
                requestCurrentViewCount();
            })
            .catch(function(err) {
                console.error("ViewCountHub: Connection failed", err);
                isConnectionReady = false;
            });
    } catch (err) {
        console.error("Error initializing ViewCountHub:", err);
    }
}

// Tăng lượt xem cho bài đăng
function increaseViewCount() {
    logDebug("increaseViewCount called with questionId:", questionId);
    
    // Kiểm tra nhiều điều kiện trước khi tăng view
    if (isViewCounted) {
        logDebug("View already counted, not increasing");
        return;
    }
    
    if (!questionId) {
        logDebug("No question ID available, cannot increase view count");
        return;
    }
    
    // Show loading indicator
    const viewLoadingIndicator = document.querySelector('.view-loading-indicator');
    if (viewLoadingIndicator) {
        viewLoadingIndicator.classList.remove('d-none');
    }
    
    // Kiểm tra localStorage trước khi tăng
    const viewedQuestions = JSON.parse(localStorage.getItem('viewedQuestions') || '{}');
    if (viewedQuestions[questionId]) {
        logDebug(`Question ${questionId} already viewed in this session (from localStorage), not counting again`);
        isViewCounted = true;
        
        // Hide loading indicator
        if (viewLoadingIndicator) {
            viewLoadingIndicator.classList.add('d-none');
        }
        
        return;
    }
    
    // Đánh dấu đã đếm ngay lập tức để tránh đếm nhiều lần
    isViewCounted = true;
    
    // Kiểm tra kết nối
    if (!window.viewCountConnection || window.viewCountConnection.state !== "Connected") {
        logDebug("Connection not ready, will retry in 1 second");
        
        setTimeout(function() {
            if (!window.viewCountConnection || window.viewCountConnection.state !== "Connected") {
                logDebug("Connection still not ready after retry");
                // Hide loading indicator
                if (viewLoadingIndicator) {
                    viewLoadingIndicator.classList.add('d-none');
                }
                return;
            }
            
            sendViewCountRequest();
        }, 1000);
        return;
    }
    
    sendViewCountRequest();
    
    function sendViewCountRequest() {
        logDebug(`Invoking IncreaseViewCount for question ${questionId}`);
        
        window.viewCountConnection.invoke("IncreaseViewCount", questionId)
            .then(function() {
                logDebug("View count increased successfully");
                
                // Lưu vào localStorage để tránh đếm lại ngay cả khi refresh trang
                const viewedQuestions = JSON.parse(localStorage.getItem('viewedQuestions') || '{}');
                viewedQuestions[questionId] = new Date().toISOString();
                localStorage.setItem('viewedQuestions', JSON.stringify(viewedQuestions));
            })
            .catch(function(err) {
                console.error("Error increasing view count:", err);
            })
            .finally(function() {
                // Hide loading indicator
                if (viewLoadingIndicator) {
                    viewLoadingIndicator.classList.add('d-none');
                }
            });
    }
}

// Cập nhật hiển thị số lượt xem trên giao diện
function updateViewCountDisplay(viewCount) {
    logDebug(`Updating view count display to: ${viewCount}`);
    
    // Tìm tất cả phần tử hiển thị lượt xem
    const viewCountElements = document.querySelectorAll('.view-count, .question-views');
    
    if (viewCountElements.length === 0) {
        logDebug("No view count elements found on page");
        return;
    }
    
    viewCountElements.forEach(function(element) {
        // Chỉ cập nhật nếu số mới khác số cũ
        const currentCount = parseInt(element.textContent.trim());
        if (isNaN(currentCount) || currentCount !== viewCount) {
            logDebug(`Updating element from ${currentCount} to ${viewCount}`);
            element.textContent = viewCount;
            
            // Thêm hiệu ứng highlight
            element.classList.add('view-count-updated');
            setTimeout(function() {
                element.classList.remove('view-count-updated');
            }, 2000);
        }
    });
    
    // Cập nhật số trong tiêu đề trang nếu có
    const title = document.querySelector('title');
    if (title && title.textContent.includes('views')) {
        const newTitle = title.textContent.replace(/\d+ views/, `${viewCount} views`);
        title.textContent = newTitle;
    }
}