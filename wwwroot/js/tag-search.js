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
    const tagCount = document.getElementById('tag-count');
    const totalTagCount = tagCards.length;
    
    if (!searchInput || !tagCards.length) return;
    
    // Initialize tag count
    if (tagCount) {
        tagCount.textContent = totalTagCount;
    }
    
    // Handle search input
    searchInput.addEventListener('input', function() {
        const searchTerm = this.value.trim().toLowerCase();
        let visibleCount = 0;
        
        // Filter tags based on search term
        tagCards.forEach(card => {
            const tagName = card.querySelector('.tag-name').textContent.toLowerCase();
            const tagDescription = card.querySelector('.tag-description')?.textContent.toLowerCase() || '';
            
            // Check if tag name or description contains the search term
            const isMatch = tagName.includes(searchTerm) || tagDescription.includes(searchTerm);
            
            // Show/hide the card based on match
            if (isMatch) {
                card.classList.remove('d-none');
                visibleCount++;
            } else {
                card.classList.add('d-none');
            }
        });
        
        // Update tag count
        if (tagCount) {
            tagCount.textContent = visibleCount;
        }
        
        // Show/hide no results message
        if (noResultsMessage) {
            if (visibleCount === 0 && searchTerm !== '') {
                noResultsMessage.classList.remove('d-none');
            } else {
                noResultsMessage.classList.add('d-none');
            }
        }
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
}); 