/**
 * SignalR Loader - Đảm bảo tải SignalR library trước khi sử dụng
 */

(function() {
    // Sự kiện custom để thông báo SignalR đã sẵn sàng
    const SIGNALR_READY_EVENT = 'signalr-loaded';

    // Cờ trạng thái tải
    let isLoading = false;
    let isLoaded = false;
    
    // Kiểm tra xem SignalR đã tải chưa khi trang được tải
    document.addEventListener('DOMContentLoaded', function() {
        console.log('SignalR Loader: Checking if SignalR is available');
        if (typeof signalR !== 'undefined') {
            console.log('SignalR Loader: SignalR already available');
            markAsLoaded();
        } else {
            // Thử tải SignalR nếu chưa có
            loadSignalR();
        }
    });

    /**
     * Tải thư viện SignalR từ CDN hoặc local fallback
     */
    function loadSignalR() {
        if (isLoading || isLoaded) return;
        
        isLoading = true;
        console.log('SignalR Loader: Loading SignalR library');
        
        // Tạo script element để tải SignalR
        const script = document.createElement('script');
        script.type = 'text/javascript';
        
        // Thiết lập callback cho trường hợp tải thành công và thất bại
        script.onload = function() {
            console.log('SignalR Loader: SignalR loaded successfully');
            markAsLoaded();
        };
        
        script.onerror = function() {
            console.error('SignalR Loader: Failed to load SignalR from CDN, trying local fallback');
            loadLocalSignalR();
        };
        
        // Thử tải từ CDN trước
        script.src = 'https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/7.0.5/signalr.min.js';
        document.head.appendChild(script);
    }
    
    /**
     * Fallback để tải SignalR từ file local nếu CDN thất bại
     */
    function loadLocalSignalR() {
        const script = document.createElement('script');
        script.type = 'text/javascript';
        
        script.onload = function() {
            console.log('SignalR Loader: SignalR loaded from local source');
            markAsLoaded();
        };
        
        script.onerror = function() {
            console.error('SignalR Loader: Failed to load SignalR from local source');
            isLoading = false;
        };
        
        script.src = '/lib/signalr/signalr.min.js';
        document.head.appendChild(script);
    }
    
    /**
     * Đánh dấu SignalR đã tải và kích hoạt các listener
     */
    function markAsLoaded() {
        if (isLoaded) return;
        
        isLoaded = true;
        isLoading = false;
        
        // Tạo và phân phối sự kiện custom để thông báo SignalR đã sẵn sàng
        const event = new CustomEvent(SIGNALR_READY_EVENT);
        document.dispatchEvent(event);
        
        console.log('SignalR Loader: Ready event dispatched');
    }
    
    /**
     * API công khai để kiểm tra trạng thái tải
     */
    window.SignalRLoader = {
        // Kiểm tra nếu SignalR đã tải chưa
        isLoaded: function() {
            return isLoaded || typeof signalR !== 'undefined';
        },
        
        // Tải SignalR theo yêu cầu
        load: function() {
            loadSignalR();
        },
        
        // Đăng ký callback để thực thi khi SignalR đã sẵn sàng
        ready: function(callback) {
            if (typeof callback !== 'function') return;
            
            if (this.isLoaded()) {
                // Nếu đã tải, thực thi callback ngay lập tức
                callback();
            } else {
                // Nếu chưa tải, đăng ký sự kiện để thực thi khi tải xong
                document.addEventListener(SIGNALR_READY_EVENT, callback);
                
                // Đảm bảo thư viện được tải nếu chưa bắt đầu tải
                if (!isLoading) {
                    loadSignalR();
                }
            }
        }
    };
})(); 