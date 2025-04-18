﻿// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Theme toggle functionality
document.addEventListener('DOMContentLoaded', function () {
    // Check for saved theme preference or use the system preference
    const savedTheme = localStorage.getItem('theme');
    
    if (savedTheme) {
        document.documentElement.setAttribute('data-bs-theme', savedTheme);
        if (savedTheme === 'dark') {
            document.getElementById('checkbox')?.setAttribute('checked', 'checked');
        }
    } else {
        // Check if user prefers dark mode
        const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
        if (prefersDark) {
            document.documentElement.setAttribute('data-bs-theme', 'dark');
            document.getElementById('checkbox')?.setAttribute('checked', 'checked');
            localStorage.setItem('theme', 'dark');
        }
    }

    // Add event listener to theme toggle
    const themeToggle = document.getElementById('checkbox');
    
    if (themeToggle) {
        themeToggle.addEventListener('change', function (e) {
            if (e.target.checked) {
                document.documentElement.setAttribute('data-bs-theme', 'dark');
                localStorage.setItem('theme', 'dark');
            } else {
                document.documentElement.setAttribute('data-bs-theme', 'light');
                localStorage.setItem('theme', 'light');
            }
        });
    }
});

// Question pagination handling
document.addEventListener('DOMContentLoaded', function () {
    // Handle tab activation in questions index
    const urlParams = new URLSearchParams(window.location.search);
    const tab = urlParams.get('tab');
    
    if (tab) {
        try {
            const tabElement = document.querySelector(`.nav-link[href*="tab=${tab}"]`);
            if (tabElement) {
                tabElement.classList.add('active');
                // Remove active class from other tabs
                document.querySelectorAll('.nav-link.active').forEach(el => {
                    if (el !== tabElement && !el.getAttribute('href').includes('tab=')) {
                        el.classList.remove('active');
                    }
                });
            }
        } catch (e) {
            console.error("Error handling tab activation:", e);
        }
    }
});

// Format dates with relative time
document.addEventListener('DOMContentLoaded', function () {
    const timeElements = document.querySelectorAll('.relative-time');
    
    timeElements.forEach(function (element) {
        const datetime = new Date(element.getAttribute('datetime'));
        element.textContent = formatRelativeTime(datetime);
    });
    
    function formatRelativeTime(date) {
        const now = new Date();
        const diffInSeconds = Math.floor((now - date) / 1000);
        
        if (diffInSeconds < 60) {
            return 'just now';
        }
        
        const diffInMinutes = Math.floor(diffInSeconds / 60);
        if (diffInMinutes < 60) {
            return `${diffInMinutes} ${diffInMinutes === 1 ? 'minute' : 'minutes'} ago`;
        }
        
        const diffInHours = Math.floor(diffInMinutes / 60);
        if (diffInHours < 24) {
            return `${diffInHours} ${diffInHours === 1 ? 'hour' : 'hours'} ago`;
        }
        
        const diffInDays = Math.floor(diffInHours / 24);
        if (diffInDays < 30) {
            return `${diffInDays} ${diffInDays === 1 ? 'day' : 'days'} ago`;
        }
        
        const diffInMonths = Math.floor(diffInDays / 30);
        if (diffInMonths < 12) {
            return `${diffInMonths} ${diffInMonths === 1 ? 'month' : 'months'} ago`;
        }
        
        const diffInYears = Math.floor(diffInMonths / 12);
        return `${diffInYears} ${diffInYears === 1 ? 'year' : 'years'} ago`;
    }
});

// Elements
const themeToggle = document.getElementById('themeToggle');
const htmlElement = document.documentElement;

// Function to set theme
function setTheme(theme) {
    if (theme === 'dark') {
        htmlElement.setAttribute('data-bs-theme', 'dark');
        localStorage.setItem('theme', 'dark');
        if (themeToggle) {
            themeToggle.checked = true;
        }
    } else {
        htmlElement.setAttribute('data-bs-theme', 'light');
        localStorage.setItem('theme', 'light');
        if (themeToggle) {
            themeToggle.checked = false;
        }
    }
}

// Check for saved theme preference or respect OS preference
document.addEventListener('DOMContentLoaded', () => {
    const savedTheme = localStorage.getItem('theme');
    
    if (savedTheme) {
        setTheme(savedTheme);
    } else {
        const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
        setTheme(prefersDark ? 'dark' : 'light');
    }
    
    // Theme toggle event listener
    if (themeToggle) {
        themeToggle.addEventListener('change', (e) => {
            setTheme(e.target.checked ? 'dark' : 'light');
        });
    }
    
    // Initialize tooltips
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
    
    // Initialize popovers
    const popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    popoverTriggerList.map(function (popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl);
    });
    
    // Auto-resize textareas to fit content
    document.querySelectorAll('textarea.auto-resize').forEach(textarea => {
        textarea.style.height = 'auto';
        textarea.style.height = textarea.scrollHeight + 'px';
        
        textarea.addEventListener('input', () => {
            textarea.style.height = 'auto';
            textarea.style.height = textarea.scrollHeight + 'px';
        });
    });
    
    // Mark All Notifications as Read
    const markAllReadBtn = document.getElementById('markAllReadBtn');
    if (markAllReadBtn) {
        markAllReadBtn.addEventListener('click', (e) => {
            e.preventDefault();
            e.stopPropagation();
            
            fetch('/Notifications/MarkAllAsRead', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                },
                credentials: 'include'
            })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    // Update UI to mark all notifications as read
                    document.querySelectorAll('.notification-item.unread').forEach(item => {
                        item.classList.remove('unread');
                    });
                    
                    // Reset badge count
                    const badge = document.querySelector('.notification-badge');
                    if (badge) {
                        badge.style.display = 'none';
                    }
                }
            })
            .catch(error => console.error('Error marking notifications as read:', error));
        });
    }
    
    // Initialize code syntax highlighting
    initCodeHighlighting();
    
    // Handle sidebar position relative to footer
    const handleSidebarFooterPosition = () => {
        const footer = document.querySelector('.footer');
        const sidebars = document.querySelectorAll('.sidebar, .right-sidebar');
        
        if (!footer || sidebars.length === 0) return;
        
        const footerRect = footer.getBoundingClientRect();
        const windowHeight = window.innerHeight;
        
        sidebars.forEach(sidebar => {
            const sidebarRect = sidebar.getBoundingClientRect();
            const sidebarHeight = sidebarRect.height;
            
            // If footer is in viewport, make sure sidebar doesn't overlap it
            if (footerRect.top < windowHeight) {
                const distanceToFooter = footerRect.top - windowHeight;
                const newMaxHeight = windowHeight - Math.abs(distanceToFooter) - 20; // 20px buffer
                
                // Limit sidebar height so it doesn't overlap with footer
                if (sidebarHeight > newMaxHeight && newMaxHeight > 200) { // Keep minimum 200px height
                    sidebar.style.maxHeight = newMaxHeight + 'px';
                }
            } else {
                // Reset when footer is not in viewport
                const navbarHeight = document.querySelector('.navbar')?.offsetHeight || 70;
                sidebar.style.maxHeight = `calc(100vh - ${navbarHeight + 16}px)`;
            }
        });
    };
    
    // Call the function initially and on scroll
    handleSidebarFooterPosition();
    window.addEventListener('scroll', handleSidebarFooterPosition, { passive: true });
    window.addEventListener('resize', handleSidebarFooterPosition, { passive: true });
});

// Enhanced sidebar-footer interaction handling
function setupSidebarFooterInteraction() {
    const footer = document.querySelector('.footer');
    const sidebars = document.querySelectorAll('.sidebar, .right-sidebar');
    
    if (!footer || sidebars.length === 0) return;
    
    // Track last scroll position to determine scroll direction
    let lastScrollTop = window.pageYOffset || document.documentElement.scrollTop;
    
    function handleScroll() {
        const footerRect = footer.getBoundingClientRect();
        const windowHeight = window.innerHeight;
        const currentScrollTop = window.pageYOffset || document.documentElement.scrollTop;
        const scrollingDown = currentScrollTop > lastScrollTop;
        
        // Update last scroll position
        lastScrollTop = currentScrollTop;
        
        sidebars.forEach(sidebar => {
            const sidebarRect = sidebar.getBoundingClientRect();
            const sidebarHeight = sidebarRect.height;
            const sidebarBottom = sidebarRect.bottom;
            
            // Distance between bottom of sidebar and top of footer
            const distanceToFooter = footerRect.top - sidebarBottom;
            
            // If sidebar is about to overlap with footer
            if (footerRect.top <= windowHeight && distanceToFooter < 20) {
                // Add visual indication that sidebar has reached footer
                sidebar.classList.add('reached-footer');
                
                // Calculate new max height to prevent footer overlap
                const newMaxHeight = windowHeight - (Math.abs(distanceToFooter) + 20);
                
                // Only adjust height if it would still be reasonably tall
                if (newMaxHeight > 200) {
                    sidebar.style.maxHeight = `${newMaxHeight}px`;
                }
                
                // If user is scrolling up and sidebar is shorter than window
                if (!scrollingDown && sidebarHeight < (windowHeight - 100)) {
                    // Allow sidebar to expand upward a bit for better UX
                    const navbarHeight = document.querySelector('.navbar')?.offsetHeight || 70;
                    sidebar.style.top = `${navbarHeight + 6}px`;
                }
            } else {
                // Reset styles when not near footer
                sidebar.classList.remove('reached-footer');
                const navbarHeight = document.querySelector('.navbar')?.offsetHeight || 70;
                sidebar.style.maxHeight = `calc(100vh - ${navbarHeight + 16}px)`;
                sidebar.style.top = `${navbarHeight + 6}px`;
            }
        });
    }
    
    // Add scroll event listener
    window.addEventListener('scroll', handleScroll, { passive: true });
    
    // Call once on page load
    handleScroll();
    
    // Also handle on resize
    window.addEventListener('resize', handleScroll, { passive: true });
}

// Call the function when DOM is loaded
document.addEventListener('DOMContentLoaded', setupSidebarFooterInteraction);

// Handle Form Validation
(function () {
    'use strict';
    
    // Fetch all forms we want to apply custom validation to
    const forms = document.querySelectorAll('.needs-validation');
    
    // Loop over them and prevent submission
    Array.from(forms).forEach(form => {
        form.addEventListener('submit', event => {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }
            
            form.classList.add('was-validated');
        }, false);
    });
})();

// Save Item Functionality
function toggleSaveItem(itemId, itemType) {
    fetch('/SavedItems/Toggle', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-Requested-With': 'XMLHttpRequest'
        },
        body: JSON.stringify({ itemId, itemType }),
        credentials: 'include'
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            const saveButton = document.querySelector(`.save-button[data-id="${itemId}"][data-type="${itemType}"]`);
            if (saveButton) {
                if (data.isSaved) {
                    saveButton.classList.add('saved');
                    saveButton.querySelector('i').classList.remove('bi-bookmark');
                    saveButton.querySelector('i').classList.add('bi-bookmark-fill');
                    saveButton.setAttribute('title', 'Remove from saved items');
                } else {
                    saveButton.classList.remove('saved');
                    saveButton.querySelector('i').classList.remove('bi-bookmark-fill');
                    saveButton.querySelector('i').classList.add('bi-bookmark');
                    saveButton.setAttribute('title', 'Save this item');
                }
                
                // Update tooltip if bootstrap tooltip is initialized
                if (bootstrap.Tooltip.getInstance(saveButton)) {
                    bootstrap.Tooltip.getInstance(saveButton).dispose();
                    new bootstrap.Tooltip(saveButton);
                }
            }
        }
    })
    .catch(error => console.error('Error toggling saved item:', error));
}

// Initialize Code Syntax Highlighting
function initCodeHighlighting() {
    // Add language indicator to code blocks
    document.querySelectorAll('pre').forEach(preElement => {
        const codeElement = preElement.querySelector('code');
        if (codeElement) {
            // Extract language from class
            const langMatch = codeElement.className.match(/language-(\w+)/);
            if (langMatch && langMatch[1]) {
                const language = langMatch[1];
                
                // Set data-language attribute for styling
                preElement.setAttribute('data-language', language);
                
                // Add copy button
                addCopyButtonToCodeBlock(preElement);
            }
        }
    });
    
    // If Prism is loaded, highlight code blocks
    if (typeof Prism !== 'undefined') {
        Prism.highlightAll();
    }
}

// Add copy button to code blocks
function addCopyButtonToCodeBlock(preElement) {
    // Create copy button
    const copyButton = document.createElement('button');
    copyButton.innerHTML = '<i class="bi bi-clipboard"></i>';
    copyButton.className = 'copy-code-button';
    copyButton.title = 'Copy to clipboard';
    
    // Add click event
    copyButton.addEventListener('click', function() {
        const code = preElement.querySelector('code').innerText;
        
        navigator.clipboard.writeText(code).then(() => {
            // Show success feedback
            copyButton.innerHTML = '<i class="bi bi-clipboard-check"></i>';
            copyButton.classList.add('copied');
            
            // Reset after 2 seconds
            setTimeout(() => {
                copyButton.innerHTML = '<i class="bi bi-clipboard"></i>';
                copyButton.classList.remove('copied');
            }, 2000);
        })
        .catch(err => {
            console.error('Failed to copy code: ', err);
            copyButton.innerHTML = '<i class="bi bi-clipboard-x"></i>';
            
            // Reset after 2 seconds
            setTimeout(() => {
                copyButton.innerHTML = '<i class="bi bi-clipboard"></i>';
            }, 2000);
        });
    });
    
    // Add to pre element
    preElement.appendChild(copyButton);
}

// Vote notification handler
let notificationConnection = null;

document.addEventListener('DOMContentLoaded', function() {
    // Đảm bảo chỉ khởi tạo thông báo sau khi SignalR đã được tải
    if (window.SignalRLoader) {
        SignalRLoader.ready(function() {
            console.log("SignalR loaded successfully, initializing notification hub in site.js");
            // Initialize SignalR connection for notifications
            initializeNotificationHub();
            
            // Initialize any toast elements
            initializeToasts();
        });
    } else {
        console.error("SignalRLoader not found. Make sure signalr-loader.js is loaded first.");
        // Vẫn khởi tạo toast elements
        initializeToasts();
    }
});

// Initialize the notification hub connection
function initializeNotificationHub() {
    // Sử dụng kết nối đã được khởi tạo từ signalr-connection-check.js hoặc notification-service-fixed.js
    if (window.notificationConnection) {
        console.log("Using existing NotificationHub connection from window object");
        notificationConnection = window.notificationConnection;
        
        // Đảm bảo đã thiết lập listener cho ReceiveNotification
        setupVoteNotificationListener();
        return;
    }
    
    // Nếu không có kết nối sẵn, kiểm tra đăng nhập trước khi kết nối
    const isLoggedIn = document.querySelector('form#logoutForm') !== null || 
                        document.querySelector('.dropdown-item[onclick*="logoutForm"]') !== null;
                        
    if (!isLoggedIn) {
        console.log("User not logged in. NotificationHub requires authentication.");
        return;
    }
    
    // Đảm bảo SignalR đã được tải
    if (typeof signalR === 'undefined') {
        console.error("SignalR is not defined. Cannot create NotificationHub connection.");
        return;
    }
    
    // Create SignalR connection to notification hub
    notificationConnection = new signalR.HubConnectionBuilder()
        .withUrl("/notificationHub")
        .withAutomaticReconnect()
        .build();
    
    // Lưu kết nối vào đối tượng window để các script khác có thể sử dụng
    window.notificationConnection = notificationConnection;
    
    // Thiết lập listener cho thông báo
    setupVoteNotificationListener();
    
    // Start the connection
    notificationConnection.start()
        .then(function() {
            console.log("NotificationHub connected");
        })
        .catch(function(err) {
            console.error("NotificationHub connection error:", err);
        });
}

// Thiết lập listener cho thông báo vote
function setupVoteNotificationListener() {
    // Xóa listener cũ để tránh trùng lặp
    notificationConnection.off("ReceiveNotification");
    
    // Handle receiving notifications
    notificationConnection.on("ReceiveNotification", function(notification) {
        // Display toast for vote notifications
        if (notification.type === "vote") {
            showToast("Notification", notification.message, "success");
            
            // Update UI if the user is viewing the relevant question
            if (notification.questionId) {
                // Check if we're on the question details page
                const questionIdElement = document.querySelector('meta[name="question-id"]');
                if (questionIdElement && questionIdElement.content === notification.questionId.toString()) {
                    // Update score display for real-time feedback
                    updateScoreDisplay(notification);
                }
            }
        }
    });
}

// Update score display based on notification
function updateScoreDisplay(notification) {
    if (notification.questionId && !notification.answerId) {
        // Update question score
        const scoreElement = document.querySelector(`.vote-count[data-id="${notification.questionId}"][data-type="question"]`);
        if (scoreElement) {
            scoreElement.textContent = notification.score;
            
            // Add a highlight effect
            scoreElement.classList.add('score-updated');
            setTimeout(() => {
                scoreElement.classList.remove('score-updated');
            }, 2000);
        }
    } else if (notification.answerId) {
        // Update answer score
        const scoreElement = document.querySelector(`.vote-count[data-id="${notification.answerId}"][data-type="answer"]`);
        if (scoreElement) {
            scoreElement.textContent = notification.score;
            
            // Add a highlight effect
            scoreElement.classList.add('score-updated');
            setTimeout(() => {
                scoreElement.classList.remove('score-updated');
            }, 2000);
        }
    }
}

// Show toast notification
function showToast(title, message, type = "info") {
    // Check if toast container exists, if not create it
    let toastContainer = document.getElementById('toast-container');
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.id = 'toast-container';
        toastContainer.className = 'toast-container position-fixed bottom-0 end-0 p-3';
        document.body.appendChild(toastContainer);
    }
    
    // Create toast ID
    const toastId = 'toast-' + Date.now();
    
    // Create toast HTML
    const toastHtml = `
        <div id="${toastId}" class="toast" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="toast-header bg-${type} text-white">
                <strong class="me-auto">${title}</strong>
                <small>${new Date().toLocaleTimeString()}</small>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
            <div class="toast-body">
                ${message}
            </div>
        </div>
    `;
    
    // Add toast to container
    toastContainer.insertAdjacentHTML('beforeend', toastHtml);
    
    // Initialize and show the toast
    const toastElement = document.getElementById(toastId);
    const toast = new bootstrap.Toast(toastElement, {
        autohide: true,
        delay: 5000
    });
    toast.show();
    
    // Remove toast from DOM after it's hidden
    toastElement.addEventListener('hidden.bs.toast', function() {
        toastElement.remove();
    });
}

// Initialize Bootstrap toasts
function initializeToasts() {
    // Add CSS for toast animations and highlighting
    const style = document.createElement('style');
    style.textContent = `
        .score-updated {
            animation: score-pulse 2s ease-in-out;
        }
        
        @keyframes score-pulse {
            0% { color: inherit; transform: scale(1); }
            50% { color: var(--bs-success); transform: scale(1.2); }
            100% { color: inherit; transform: scale(1); }
        }
        
        .toast-container {
            z-index: 1090;
        }
        
        .toast {
            opacity: 0;
            animation: toast-fade-in 0.3s ease forwards;
        }
        
        @keyframes toast-fade-in {
            from { opacity: 0; transform: translateY(20px); }
            to { opacity: 1; transform: translateY(0); }
        }
    `;
    document.head.appendChild(style);
}

// Handle Vote Actions
function vote(itemId, itemType, voteType) {
    fetch('/Vote/Cast', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-Requested-With': 'XMLHttpRequest'
        },
        body: JSON.stringify({ itemId, itemType, voteType }),
        credentials: 'include'
    })
    .then(response => {
        // Check for non-200 status
        if (!response.ok) {
            if (response.status === 401) {
                showToast("Authentication Error", "You must be logged in to vote", "danger");
                return null;
            } else {
                return response.json().then(data => {
                    throw new Error(data.message || "Error processing vote");
                });
            }
        }
        return response.json();
    })
    .then(data => {
        if (!data) return; // For 401 case, we already showed the toast
        
        if (data.success) {
            // Update vote count display
            const voteCountElement = document.querySelector(`.vote-count[data-id="${itemId}"][data-type="${itemType}"]`);
            if (voteCountElement) {
                // Apply the score and highlight effect
                voteCountElement.textContent = data.newScore;
                voteCountElement.classList.add('score-updated');
                setTimeout(() => {
                    voteCountElement.classList.remove('score-updated');
                }, 2000);
                
                // Update button states
                const upvoteBtn = document.querySelector(`.vote-btn-up[data-id="${itemId}"][data-type="${itemType}"]`);
                const downvoteBtn = document.querySelector(`.vote-btn-down[data-id="${itemId}"][data-type="${itemType}"]`);
                
                if (upvoteBtn && downvoteBtn) {
                    // Reset both buttons
                    upvoteBtn.classList.remove('active');
                    downvoteBtn.classList.remove('active');
                    
                    // Set active state based on new vote
                    if (data.userVote === 1) {
                        upvoteBtn.classList.add('active');
                        showToast("Vote Success", "Your upvote has been recorded", "success");
                    } else if (data.userVote === -1) {
                        downvoteBtn.classList.add('active');
                        showToast("Vote Success", "Your downvote has been recorded", "success");
                    } else {
                        showToast("Vote Removed", "Your vote has been removed", "info");
                    }
                }
            }
        } else {
            // Show error toast
            if (data.message) {
                showToast("Vote Error", data.message, "danger");
            }
        }
    })
    .catch(error => {
        console.error('Error casting vote:', error);
        showToast("Error", error.message || "An error occurred while processing your vote", "danger");
    });
}

// Progressive Image Loading
document.addEventListener('DOMContentLoaded', function() {
    const lazyImages = document.querySelectorAll('.lazy-load');
    
    if ('IntersectionObserver' in window) {
        const imageObserver = new IntersectionObserver((entries, observer) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const image = entry.target;
                    image.src = image.dataset.src;
                    image.classList.remove('lazy-load');
                    image.classList.add('fade-in');
                    imageObserver.unobserve(image);
                }
            });
        });
        
        lazyImages.forEach(image => {
            imageObserver.observe(image);
        });
    } else {
        // Fallback for browsers without IntersectionObserver
        lazyImages.forEach(image => {
            image.src = image.dataset.src;
            image.classList.remove('lazy-load');
        });
    }
});

// Global Search with Auto-suggestions
document.addEventListener('DOMContentLoaded', function() {
    initGlobalSearch();
});

function initGlobalSearch() {
    const searchInput = document.getElementById('globalSearchInput');
    const searchButton = document.getElementById('globalSearchButton');
    const searchDropdown = document.getElementById('searchSuggestionsDropdown');
    const loadingIndicator = document.getElementById('searchLoadingIndicator');
    const noResultsMessage = document.getElementById('noResultsMessage');
    const viewAllButton = document.getElementById('viewAllSearchResults');
    
    if (!searchInput) return; // Exit if search elements don't exist
    
    let debounceTimeout;
    let currentSearchTerm = '';
    
    // Function to show the dropdown
    function showDropdown() {
        searchDropdown.classList.add('active');
    }
    
    // Function to hide the dropdown
    function hideDropdown() {
        searchDropdown.classList.remove('active');
    }
    
    // Function to perform search
    function performSearch(searchTerm) {
        if (searchTerm.length < 2) {
            hideDropdown();
            return;
        }
        
        currentSearchTerm = searchTerm;
        showDropdown();
        showLoading();
        
        // Create URL for the API endpoint
        const apiUrl = `/api/search?q=${encodeURIComponent(searchTerm)}`;
        
        // Make the API request
        fetch(apiUrl)
            .then(response => {
                if (!response.ok) {
                    throw new Error('Network response was not ok');
                }
                return response.json();
            })
            .then(data => {
                // Process only if search term hasn't changed
                if (currentSearchTerm === searchTerm) {
                    displayResults(data, searchTerm);
                }
            })
            .catch(error => {
                console.error('Search error:', error);
                hideLoading();
                showNoResults();
            });
    }
    
    // Show loading indicator
    function showLoading() {
        loadingIndicator.classList.remove('d-none');
        document.querySelectorAll('.search-result-section').forEach(el => {
            el.style.display = 'none';
        });
        noResultsMessage.classList.add('d-none');
    }
    
    // Hide loading indicator
    function hideLoading() {
        loadingIndicator.classList.add('d-none');
    }
    
    // Show no results message
    function showNoResults() {
        document.querySelectorAll('.search-result-section').forEach(el => {
            el.style.display = 'none';
        });
        noResultsMessage.classList.remove('d-none');
    }
    
    // Display search results in the dropdown
    function displayResults(data, searchTerm) {
        hideLoading();
        
        const hasQuestions = data.questions && data.questions.length > 0;
        const hasTags = data.tags && data.tags.length > 0;
        const hasUsers = data.users && data.users.length > 0;
        const hasRepos = data.repositories && data.repositories.length > 0;
        
        // Check if we have any results at all
        if (!hasQuestions && !hasTags && !hasUsers && !hasRepos) {
            showNoResults();
            return;
        }
        
        // Hide no results message
        noResultsMessage.classList.add('d-none');
        
        // Update questions section
        const questionSection = document.getElementById('questionResults');
        const questionList = document.getElementById('questionResultsList');
        if (hasQuestions) {
            questionList.innerHTML = '';
            data.questions.forEach(question => {
                const item = document.createElement('a');
                item.href = `/Questions/Details/${question.id}`;
                item.className = 'search-result-item';
                
                // Highlight matching text
                const titleWithHighlight = highlightText(question.title, searchTerm);
                
                item.innerHTML = `
                    <div class="result-icon">
                        <i class="bi bi-question-circle text-primary"></i>
                    </div>
                    <div class="d-flex flex-column">
                        <p class="result-title">${titleWithHighlight}</p>
                        <p class="result-subtitle">${question.answerCount} answers · ${question.viewCount} views</p>
                    </div>
                `;
                questionList.appendChild(item);
            });
            questionSection.style.display = 'block';
        } else {
            questionSection.style.display = 'none';
        }
        
        // Update tags section
        const tagSection = document.getElementById('tagResults');
        const tagList = document.getElementById('tagResultsList');
        if (hasTags) {
            tagList.innerHTML = '';
            const tagsContainer = document.createElement('div');
            tagsContainer.className = 'd-flex flex-wrap p-2';
            
            data.tags.forEach(tag => {
                const tagItem = document.createElement('a');
                tagItem.href = `/Questions/Tagged/${tag.name}`;
                tagItem.className = 'search-result-tag';
                
                // Highlight matching text
                const tagNameWithHighlight = highlightText(tag.name, searchTerm);
                
                tagItem.innerHTML = `
                    <i class="bi bi-tag me-1"></i>
                    <span>${tagNameWithHighlight}</span>
                `;
                tagsContainer.appendChild(tagItem);
            });
            
            tagList.appendChild(tagsContainer);
            tagSection.style.display = 'block';
        } else {
            tagSection.style.display = 'none';
        }
        
        // Update users section
        const userSection = document.getElementById('userResults');
        const userList = document.getElementById('userResultsList');
        if (hasUsers) {
            userList.innerHTML = '';
            data.users.forEach(user => {
                const item = document.createElement('a');
                item.href = `/Users/Profile/${user.id}`;
                item.className = 'search-result-item';
                
                // Highlight matching text
                const nameWithHighlight = highlightText(user.displayName, searchTerm);
                
                item.innerHTML = `
                    <img src="${user.avatar || '/images/default-avatar.png'}" alt="${user.displayName}" class="result-avatar">
                    <div class="d-flex flex-column">
                        <p class="result-title">${nameWithHighlight}</p>
                        <p class="result-subtitle">@${user.username}</p>
                    </div>
                `;
                userList.appendChild(item);
            });
            userSection.style.display = 'block';
        } else {
            userSection.style.display = 'none';
        }
        
        // Update repositories section
        const repoSection = document.getElementById('repoResults');
        const repoList = document.getElementById('repoResultsList');
        if (hasRepos) {
            repoList.innerHTML = '';
            data.repositories.forEach(repo => {
                const item = document.createElement('a');
                item.href = `/Repository/Details/${repo.id}`;
                item.className = 'search-result-item';
                
                // Highlight matching text
                const nameWithHighlight = highlightText(repo.name, searchTerm);
                
                // Format description or show placeholder
                const description = repo.description 
                    ? repo.description.substring(0, 50) + (repo.description.length > 50 ? '...' : '') 
                    : 'No description available';
                
                // Format date if available
                const dateDisplay = repo.createdDate 
                    ? `Created: ${new Date(repo.createdDate).toLocaleDateString()}` 
                    : '';
                    
                // Owner info if available
                const ownerInfo = repo.owner ? `by ${repo.owner}` : '';
                
                item.innerHTML = `
                    <div class="result-icon">
                        <i class="bi bi-git text-success"></i>
                    </div>
                    <div class="d-flex flex-column">
                        <p class="result-title">${nameWithHighlight}</p>
                        <p class="result-subtitle">${description}</p>
                        <small class="text-muted">${ownerInfo} ${dateDisplay}</small>
                    </div>
                `;
                repoList.appendChild(item);
            });
            repoSection.style.display = 'block';
        } else {
            repoSection.style.display = 'none';
        }
        
        // Update view all results button link
        viewAllButton.href = `/Questions/Index?search=${encodeURIComponent(searchTerm)}`;
    }
    
    // Function to highlight matching text parts
    function highlightText(text, query) {
        if (!text) return '';
        
        // Escape special regex characters
        const escapedQuery = query.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
        const regex = new RegExp(`(${escapedQuery})`, 'gi');
        
        return text.replace(regex, '<span class="highlight">$1</span>');
    }
    
    // Event listeners
    searchInput.addEventListener('input', function(e) {
        const searchTerm = e.target.value.trim();
        
        // Clear any existing timeout
        if (debounceTimeout) {
            clearTimeout(debounceTimeout);
        }
        
        // Only search if there's at least 2 characters
        if (searchTerm.length >= 2) {
            // Add debounce to avoid making too many requests
            debounceTimeout = setTimeout(() => {
                performSearch(searchTerm);
            }, 300); // 300ms delay
        } else if (searchTerm.length === 0) {
            hideDropdown();
        }
    });
    
    // Handle clicks outside to close dropdown
    document.addEventListener('click', function(e) {
        // Check if click is outside the search container
        if (!e.target.closest('#globalSearchContainer') && searchDropdown.classList.contains('active')) {
            hideDropdown();
        }
    });
    
    // Handle "View all results" button
    if (viewAllButton) {
        viewAllButton.addEventListener('click', function(e) {
            const searchTerm = searchInput.value.trim();
            if (searchTerm.length >= 2) {
                window.location.href = `/Questions/Index?search=${encodeURIComponent(searchTerm)}`;
            }
        });
    }
    
    // Close dropdown when pressing Escape
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape' && searchDropdown.classList.contains('active')) {
            hideDropdown();
        }
    });
    
    // Handle form submission
    const searchForm = searchInput.closest('form');
    if (searchForm) {
        searchForm.addEventListener('submit', function(e) {
            const searchTerm = searchInput.value.trim();
            if (searchTerm.length < 2) {
                e.preventDefault();
                return;
            }
            // Default form action will execute
        });
    }
}
