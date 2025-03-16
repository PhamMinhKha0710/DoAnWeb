// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
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
