/**
 * view-counter.js
 * JavaScript module để quản lý việc tăng lượt xem khi người dùng cuộn đến giữa trang
 */

// Tạo kết nối SignalR
let connection = new signalR.HubConnectionBuilder()
    .withUrl("/viewCountHub")
    .withAutomaticReconnect()
    .build();

// Khởi tạo biến kiểm soát trạng thái
let hasIncrementedView = false;
let isConnectionReady = false;
let questionId = null;
let pageHeight = 0;
let middlePoint = 0;
let isScrollListenerActive = false;

// Hàm kiểm tra có nên tăng lượt xem hay không
function shouldIncrementView() {
    // Đã tăng lượt xem trong phiên này chưa
    if (hasIncrementedView) {
        console.log("View already incremented in this session");
        return false;
    }

    // Kiểm tra localStorage để tránh tăng lượt xem nhiều lần cho cùng một phiên
    const viewedQuestions = JSON.parse(localStorage.getItem('viewedQuestions') || '{}');
    if (viewedQuestions[questionId]) {
        console.log("Question already viewed according to localStorage");
        return false;
    }

    console.log("Should increment view: YES");
    return true;
}

// Hàm đánh dấu câu hỏi đã được xem trong localStorage
function markQuestionAsViewed() {
    console.log("Marking question as viewed in this session and localStorage");
    hasIncrementedView = true;
    const viewedQuestions = JSON.parse(localStorage.getItem('viewedQuestions') || '{}');
    viewedQuestions[questionId] = true;
    localStorage.setItem('viewedQuestions', JSON.stringify(viewedQuestions));
    console.log("Question marked as viewed successfully");
}

// Kiểm tra xem người dùng đã cuộn đến giữa trang chưa
function checkScrollPosition() {
    // Debug log - hiển thị thông tin vị trí cuộn
    console.log("Scroll check - Document height:", document.body.scrollHeight, 
                "Window height:", window.innerHeight,
                "Current position:", window.scrollY + window.innerHeight,
                "Midpoint:", middlePoint);
                
    // Kiểm tra nếu trang quá ngắn không cần cuộn
    if (document.body.scrollHeight <= window.innerHeight) {
        console.log("Page too short, waiting 5 seconds before incrementing view count");
        // Nếu đã hiển thị toàn bộ trang bao gồm điểm giữa mà không cần cuộn,
        // chỉ tăng lượt xem sau 5 giây nếu người dùng vẫn ở trang
        if (!hasIncrementedView && isConnectionReady && shouldIncrementView()) {
            setTimeout(() => {
                if (document.hasFocus()) {
                    console.log("Auto-incrementing view count after 5s");
                    incrementViewCount();
                }
            }, 5000);
        }
        return;
    }

    // Tính toán vị trí hiện tại của người dùng
    const scrollPosition = window.scrollY + window.innerHeight;
    
    // Kiểm tra xem người dùng đã cuộn qua điểm giữa của trang chưa
    if (scrollPosition >= middlePoint && shouldIncrementView() && isConnectionReady) {
        console.log("Midpoint reached! Incrementing view count");
        incrementViewCount();
    }
}

// Gửi yêu cầu tăng lượt xem đến server
function incrementViewCount() {
    if (!isConnectionReady || !questionId) {
        console.error("Cannot increment view count - Connection ready:", isConnectionReady, "Question ID:", questionId);
        return;
    }
    
    console.log("Sending request to increase view count for question ID:", questionId);
    connection.invoke("IncreaseViewCount", questionId)
        .then(() => {
            console.log("View count increased successfully");
            markQuestionAsViewed();
        })
        .catch(err => console.error("Error increasing view count:", err));
}

// Cập nhật số lượt xem trên UI
function updateViewCountDisplay(viewCount) {
    console.log("Updating view count display to:", viewCount);
    const viewCountElements = document.querySelectorAll('.view-count');
    console.log("Found", viewCountElements.length, "view count elements to update");
    viewCountElements.forEach(element => {
        element.textContent = viewCount;
    });
}

// Thiết lập sự kiện scroll
function setupScrollListener() {
    if (isScrollListenerActive) {
        console.log("Scroll listener already active, skipping setup");
        return;
    }
    
    // Tính toán chiều cao trang và điểm giữa
    pageHeight = document.body.scrollHeight;
    middlePoint = pageHeight / 2;
    
    console.log("Setting up scroll listener - Page height:", pageHeight, "Middle point:", middlePoint);
    
    // Thêm sự kiện cuộn
    window.addEventListener('scroll', checkScrollPosition);
    isScrollListenerActive = true;
    
    // Kiểm tra vị trí ban đầu (có thể đã ở giữa trang khi tải)
    console.log("Checking initial scroll position");
    checkScrollPosition();
}

// Khởi tạo việc đếm lượt xem
function initViewCounter(qId) {
    if (!qId) {
        console.error("Cannot initialize view counter - No question ID provided");
        return;
    }
    
    console.log("Initializing view counter for question ID:", qId);
    questionId = qId;
    
    // Khởi động kết nối SignalR nếu chưa
    if (connection.state !== "Connected") {
        console.log("Starting SignalR connection...");
        connection.start()
            .then(() => {
                console.log("Connected to ViewCountHub");
                isConnectionReady = true;
                
                // Lấy số lượt xem hiện tại
                console.log("Requesting current view count...");
                connection.invoke("GetCurrentViewCount", questionId);
                
                // Thiết lập sự kiện cuộn
                setupScrollListener();
            })
            .catch(err => console.error("Error connecting to ViewCountHub:", err));
    } else {
        console.log("SignalR connection already established");
        isConnectionReady = true;
        connection.invoke("GetCurrentViewCount", questionId);
        setupScrollListener();
    }
}

// Xử lý sự kiện nhận số lượt xem hiện tại
connection.on("ReceiveCurrentViewCount", (qId, viewCount) => {
    console.log("Received current view count for question ID:", qId, "Count:", viewCount);
    if (qId === questionId) {
        updateViewCountDisplay(viewCount);
    }
});

// Xử lý sự kiện nhận số lượt xem đã cập nhật
connection.on("ReceiveUpdatedViewCount", (qId, viewCount) => {
    console.log("Received updated view count for question ID:", qId, "Count:", viewCount);
    if (qId === questionId) {
        updateViewCountDisplay(viewCount);
    }
});

// Xử lý khi cửa sổ thay đổi kích thước
window.addEventListener('resize', () => {
    // Cập nhật lại các giá trị liên quan đến kích thước trang
    pageHeight = document.body.scrollHeight;
    middlePoint = pageHeight / 2;
});

// Export các hàm công khai
window.ViewCounter = {
    init: initViewCounter
}; 