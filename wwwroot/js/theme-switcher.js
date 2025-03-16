/**
 * Theme Switcher
 * Handles switching between light and dark themes
 */
document.addEventListener('DOMContentLoaded', function() {
    const toggleSwitch = document.querySelector('.theme-switch input[type="checkbox"]');
    const currentTheme = localStorage.getItem('theme');
    const lightIcon = document.querySelector('.theme-icon-light');
    const darkIcon = document.querySelector('.theme-icon-dark');
    
    // Check if a theme preference is stored
    if (currentTheme) {
        document.documentElement.setAttribute('data-theme', currentTheme);
        
        // Update the toggle switch if dark theme is active
        if (currentTheme === 'dark') {
            toggleSwitch.checked = true;
            if (lightIcon && darkIcon) {
                lightIcon.style.display = 'none';
                darkIcon.style.display = 'block';
            }
        } else {
            if (lightIcon && darkIcon) {
                lightIcon.style.display = 'block';
                darkIcon.style.display = 'none';
            }
        }
    }
    
    // Function to switch theme
    function switchTheme(e) {
        // Add transition class for smooth theme change
        document.body.classList.add('theme-transition');
        document.querySelectorAll('*').forEach(element => {
            element.classList.add('theme-transition');
        });
        
        if (e.target.checked) {
            document.documentElement.setAttribute('data-theme', 'dark');
            localStorage.setItem('theme', 'dark');
            if (lightIcon && darkIcon) {
                lightIcon.style.display = 'none';
                darkIcon.style.display = 'block';
            }
        } else {
            document.documentElement.setAttribute('data-theme', 'light');
            localStorage.setItem('theme', 'light');
            if (lightIcon && darkIcon) {
                lightIcon.style.display = 'block';
                darkIcon.style.display = 'none';
            }
        }
        
        // Preserve code formatting in markdown content
        preserveCodeFormatting();
        
        // Remove transition class after theme change is complete
        setTimeout(() => {
            document.body.classList.remove('theme-transition');
            document.querySelectorAll('*').forEach(element => {
                element.classList.remove('theme-transition');
            });
        }, 500);
    }
    
    // Function to preserve code formatting in markdown content
    function preserveCodeFormatting() {
        // Find all code blocks and preserve their formatting
        const codeBlocks = document.querySelectorAll('pre code, .markdown-body pre, .markdown-preview pre, .question-body code, .answer-body code, .content-area pre, .content-area code');
        
        codeBlocks.forEach(block => {
            // Ensure code blocks maintain their formatting
            block.style.whiteSpace = 'pre';
            block.style.tabSize = '4';
            block.style.MozTabSize = '4';
            
            // Add theme-specific styling
            if (document.documentElement.getAttribute('data-theme') === 'dark') {
                block.style.backgroundColor = '#334155';
                block.style.color = '#e2e8f0';
            } else {
                block.style.backgroundColor = '#f1f5f9';
                block.style.color = '#334155';
            }
        });
        
        // Also handle inline code elements
        const inlineCode = document.querySelectorAll('code:not(pre code)');
        inlineCode.forEach(code => {
            if (document.documentElement.getAttribute('data-theme') === 'dark') {
                code.style.backgroundColor = '#334155';
                code.style.color = '#e2e8f0';
            } else {
                code.style.backgroundColor = '#f1f5f9';
                code.style.color = '#334155';
            }
        });
    }
    
    // Apply code formatting on page load
    preserveCodeFormatting();
    
    // Listen for toggle changes
    if (toggleSwitch) {
        toggleSwitch.addEventListener('change', switchTheme, false);
    }
    
    // Also preserve code formatting when markdown preview is toggled
    const previewButtons = document.querySelectorAll('.markdown-preview-btn');
    previewButtons.forEach(button => {
        button.addEventListener('click', function() {
            // Short delay to ensure preview content is loaded
            setTimeout(preserveCodeFormatting, 100);
        });
    });
    
    // Create a MutationObserver to watch for content changes
    const observer = new MutationObserver(function(mutations) {
        mutations.forEach(function(mutation) {
            if (mutation.type === 'childList' && mutation.addedNodes.length > 0) {
                preserveCodeFormatting();
            }
        });
    });
    
    // Start observing the question and answer containers
    const questionBody = document.querySelector('.question-body');
    const answerContainer = document.querySelector('.answers-container');
    
    if (questionBody) {
        observer.observe(questionBody, { childList: true, subtree: true });
    }
    
    if (answerContainer) {
        observer.observe(answerContainer, { childList: true, subtree: true });
    }
    
    // Export the preserveCodeFormatting function to make it globally available
    window.preserveCodeFormatting = preserveCodeFormatting;
});