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