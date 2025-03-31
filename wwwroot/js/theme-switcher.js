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
        document.documentElement.setAttribute('data-bs-theme', currentTheme);
        document.body.setAttribute('data-bs-theme', currentTheme);
        
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
            document.documentElement.setAttribute('data-bs-theme', 'dark');
            document.body.setAttribute('data-bs-theme', 'dark');
            localStorage.setItem('theme', 'dark');
            if (lightIcon && darkIcon) {
                lightIcon.style.display = 'none';
                darkIcon.style.display = 'block';
            }
        } else {
            document.documentElement.setAttribute('data-theme', 'light');
            document.documentElement.setAttribute('data-bs-theme', 'light');
            document.body.setAttribute('data-bs-theme', 'light');
            localStorage.setItem('theme', 'light');
            if (lightIcon && darkIcon) {
                lightIcon.style.display = 'block';
                darkIcon.style.display = 'none';
            }
        }
        
        // Preserve code formatting in markdown content
        preserveCodeFormatting();
        
        // Update tag styling for the current theme
        updateTagStyling();
        
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
    
    // Function to update tag styling based on the current theme
    function updateTagStyling() {
        // Check the current theme
        const isDarkTheme = document.documentElement.getAttribute('data-theme') === 'dark';
        
        // Update tag card styling for dark theme
        const tagCards = document.querySelectorAll('.tag-card');
        tagCards.forEach(card => {
            if (isDarkTheme) {
                card.style.background = '#2d3748';
                card.style.borderColor = 'rgba(108, 92, 231, 0.2)';
                card.style.boxShadow = '0 6px 15px rgba(0, 0, 0, 0.15)';
            } else {
                card.style.background = 'white';
                card.style.borderColor = 'rgba(108, 92, 231, 0.05)';
                card.style.boxShadow = '0 6px 15px rgba(0, 0, 0, 0.05)';
            }
        });
        
        // Update tag text colors for dark theme
        const tagDescriptions = document.querySelectorAll('.tag-description');
        tagDescriptions.forEach(desc => {
            if (isDarkTheme) {
                desc.style.color = '#e2e8f0';
            } else {
                desc.style.color = '#4a5568';
            }
        });
        
        // Update tag action links
        const tagActionLinks = document.querySelectorAll('.tag-action-link:not(.primary)');
        tagActionLinks.forEach(link => {
            if (isDarkTheme) {
                link.style.color = '#a0aec0';
            } else {
                link.style.color = '#718096';
            }
        });
    }
    
    // Apply code formatting on page load
    preserveCodeFormatting();
    
    // Apply tag styling on page load
    updateTagStyling();
    
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
                updateTagStyling();
            }
        });
    });
    
    // Start observing the question and answer containers
    const questionBody = document.querySelector('.question-body');
    const answerContainer = document.querySelector('.answers-container');
    const tagsContainer = document.querySelector('.tags-grid-container');
    
    if (questionBody) {
        observer.observe(questionBody, { childList: true, subtree: true });
    }
    
    if (answerContainer) {
        observer.observe(answerContainer, { childList: true, subtree: true });
    }
    
    if (tagsContainer) {
        observer.observe(tagsContainer, { childList: true, subtree: true });
    }
    
    // Export the functions to make them globally available
    window.preserveCodeFormatting = preserveCodeFormatting;
    window.updateTagStyling = updateTagStyling;
});