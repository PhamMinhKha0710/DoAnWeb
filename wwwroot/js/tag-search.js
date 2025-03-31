/**
 * Tag Search Functionality
 * 
 * This script handles the search functionality for tags on the Tags Index page.
 * It provides instant filtering of tags as the user types in the search box.
 */

document.addEventListener('DOMContentLoaded', function() {
    const searchInput = document.getElementById('tag-search-input');
    const tagCards = document.querySelectorAll('.tag-card');
    const noResultsMessage = document.getElementById('no-search-results');
    const totalTagCount = tagCards.length;
    
    if (!searchInput || !tagCards.length) return;
    
    // Force render tag cards with proper styling
    function forceTagCardRendering() {
        console.log('Forcing tag card rendering with correct styling...');
        
        // Force FOUC (Flash of Unstyled Content) fix
        document.body.style.display = 'none';
        setTimeout(() => {
            document.body.style.display = '';
        }, 50);
        
        // Force reflow to apply styles
        tagCards.forEach(card => {
            card.style.display = 'none';
            // Force a reflow
            void card.offsetHeight;
            card.style.display = '';
        });
        
        // Apply theme styling directly
        applyThemeStyling();
        
        // Call theme-fix utilities if available
        if (window.themeFixUtils && typeof window.themeFixUtils.fixTagPageStyling === 'function') {
            setTimeout(() => {
                window.themeFixUtils.fixTagPageStyling();
            }, 100);
        }
    }
    
    // Ensure proper styling for the current theme
    function applyThemeStyling() {
        const isDarkTheme = document.documentElement.getAttribute('data-theme') === 'dark';
        
        // Apply appropriate styles based on theme
        tagCards.forEach(card => {
            if (card.style.display !== 'none') {
                if (isDarkTheme) {
                    card.style.background = '#2d3748';
                    card.style.borderColor = 'rgba(108, 92, 231, 0.2)';
                    card.style.boxShadow = '0 6px 15px rgba(0, 0, 0, 0.15)';
                } else {
                    card.style.background = 'white';
                    card.style.borderColor = 'rgba(108, 92, 231, 0.05)';
                    card.style.boxShadow = '0 6px 15px rgba(0, 0, 0, 0.05)';
                }
            }
        });
        
        // Also apply to tag descriptions
        document.querySelectorAll('.tag-description').forEach(desc => {
            if (isDarkTheme) {
                desc.style.color = '#e2e8f0';
            } else {
                desc.style.color = '#4a5568';
            }
        });
    }
    
    // Call force rendering on page load
    forceTagCardRendering();
    
    // Call styling on page load
    applyThemeStyling();
    
    // Listen for theme changes
    const themeObserver = new MutationObserver(mutations => {
        mutations.forEach(mutation => {
            if (mutation.attributeName === 'data-theme' || mutation.attributeName === 'data-bs-theme') {
                applyThemeStyling();
                
                // Force rerender after theme change
                setTimeout(forceTagCardRendering, 50);
            }
        });
    });
    
    // Observe theme changes
    themeObserver.observe(document.documentElement, { attributes: true });
    
    // Handle search input
    searchInput.addEventListener('input', function() {
        const searchTerm = this.value.trim().toLowerCase();
        let visibleCount = 0;
        
        // Filter tags based on search term
        tagCards.forEach(card => {
            const tagName = card.querySelector('.tag-badge').textContent.toLowerCase();
            const tagDescription = card.querySelector('.tag-description')?.textContent.toLowerCase() || '';
            
            // Check if tag name or description contains the search term
            const isMatch = tagName.includes(searchTerm) || tagDescription.includes(searchTerm);
            
            // Show/hide the card based on match
            if (isMatch) {
                card.style.display = '';
                visibleCount++;
            } else {
                card.style.display = 'none';
            }
        });
        
        // Show/hide no results message
        if (noResultsMessage) {
            if (visibleCount === 0 && searchTerm !== '') {
                noResultsMessage.classList.remove('d-none');
            } else {
                noResultsMessage.classList.add('d-none');
            }
        }
        
        // Re-apply styling to ensure visible cards are properly styled
        applyThemeStyling();
    });
    
    // Clear search when clicking the clear button
    const clearSearchButton = document.getElementById('clear-search-button');
    if (clearSearchButton) {
        clearSearchButton.addEventListener('click', function() {
            searchInput.value = '';
            searchInput.dispatchEvent(new Event('input'));
            searchInput.focus();
        });
    }
    
    // Server-side search toggle
    const toggleServerSearch = document.getElementById('toggle-server-search');
    const backToClientSearch = document.getElementById('back-to-client-search');
    const clientSearchContainer = document.querySelector('.tags-search-container');
    const serverSearchForm = document.getElementById('server-search-form');
    
    if (toggleServerSearch && backToClientSearch) {
        toggleServerSearch.addEventListener('click', function(e) {
            e.preventDefault();
            clientSearchContainer.classList.add('d-none');
            serverSearchForm.classList.remove('d-none');
        });
        
        backToClientSearch.addEventListener('click', function(e) {
            e.preventDefault();
            serverSearchForm.classList.add('d-none');
            clientSearchContainer.classList.remove('d-none');
        });
    }
    
    // Initial search if there's a query in the input
    if (searchInput.value.trim() !== '') {
        searchInput.dispatchEvent(new Event('input'));
    }
}); 