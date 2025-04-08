/**
 * Tag Suggestions Functionality
 * 
 * This script handles tag suggestions for the question creation form.
 * It provides an autocomplete-like experience when typing tag names.
 */

document.addEventListener('DOMContentLoaded', function() {
    // Elements
    const tagsInput = document.getElementById('Tags');
    const tagSuggestions = document.getElementById('tag-suggestions');
    let availableTags = [];
    
    // Return early if required elements don't exist
    if (!tagsInput || !tagSuggestions) return;
    
    // Initialize available tags
    initAvailableTags();
    
    /**
     * Initialize the available tags
     * First try to get tags from the data-tags attribute
     * If not available, fetch from the server
     */
    function initAvailableTags() {
        // Check if tags data is embedded in the page
        const availableTagsElement = document.getElementById('available-tags-data');
        if (availableTagsElement && availableTagsElement.dataset.tags) {
            try {
                availableTags = JSON.parse(availableTagsElement.dataset.tags);
                console.log('Tags loaded from page data:', availableTags.length);
            } catch (error) {
                console.error('Error parsing tags data:', error);
                // Fall back to fetching from server
                fetchTagsFromServer();
            }
        } else {
            // Fetch tags from server
            fetchTagsFromServer();
        }
    }
    
    /**
     * Fetch available tags from the server
     */
    function fetchTagsFromServer() {
        fetch('/Tags/GetTagsJson')
            .then(response => {
                if (!response.ok) {
                    throw new Error('Network response was not ok');
                }
                return response.json();
            })
            .then(data => {
                availableTags = data;
                console.log('Tags loaded from server:', availableTags.length);
            })
            .catch(error => {
                console.error('Error fetching tags:', error);
            });
    }
    
    /**
     * Handler for input event to show tag suggestions
     */
    tagsInput.addEventListener('input', function() {
        const inputValue = this.value;
        const lastTag = inputValue.split(',').pop().trim().toLowerCase();
        
        // Only show suggestions if we have at least 2 characters
        if (lastTag.length > 1) {
            // Filter available tags that match the input
            const matchingTags = availableTags.filter(tag => 
                tag.toLowerCase().includes(lastTag) && tag.toLowerCase() !== lastTag
            ).slice(0, 5); // Limit to 5 suggestions
            
            if (matchingTags.length > 0) {
                tagSuggestions.innerHTML = '';
                matchingTags.forEach(tag => {
                    const suggestionElement = document.createElement('div');
                    suggestionElement.className = 'tag-suggestion';
                    suggestionElement.innerHTML = `<i class="bi bi-tag me-2"></i>${tag}`;
                    
                    suggestionElement.addEventListener('click', function() {
                        const currentTags = tagsInput.value.split(',');
                        currentTags.pop(); // Remove the last (incomplete) tag
                        
                        // Add the selected tag
                        if (currentTags.length > 0 && currentTags[0] !== '') {
                            tagsInput.value = currentTags.join(',') + ', ' + tag;
                        } else {
                            tagsInput.value = tag;
                        }
                        
                        tagSuggestions.style.display = 'none';
                        tagsInput.focus();
                    });
                    
                    tagSuggestions.appendChild(suggestionElement);
                });
                
                tagSuggestions.style.display = 'block';
            } else {
                tagSuggestions.style.display = 'none';
            }
        } else {
            tagSuggestions.style.display = 'none';
        }
    });
    
    // Hide suggestions when clicking elsewhere
    document.addEventListener('click', function(e) {
        if (!tagsInput.contains(e.target) && !tagSuggestions.contains(e.target)) {
            tagSuggestions.style.display = 'none';
        }
    });
    
    // Handle keyboard navigation in suggestions
    tagsInput.addEventListener('keydown', function(e) {
        const suggestions = tagSuggestions.querySelectorAll('.tag-suggestion');
        if (suggestions.length === 0 || tagSuggestions.style.display === 'none') return;
        
        // Find the currently focused suggestion
        const focusedSuggestion = tagSuggestions.querySelector('.tag-suggestion:focus');
        const focusedIndex = Array.from(suggestions).indexOf(focusedSuggestion);
        
        switch (e.key) {
            case 'ArrowDown':
                e.preventDefault();
                if (focusedIndex < 0 || focusedIndex >= suggestions.length - 1) {
                    suggestions[0].focus();
                } else {
                    suggestions[focusedIndex + 1].focus();
                }
                break;
                
            case 'ArrowUp':
                e.preventDefault();
                if (focusedIndex <= 0) {
                    suggestions[suggestions.length - 1].focus();
                } else {
                    suggestions[focusedIndex - 1].focus();
                }
                break;
                
            case 'Escape':
                e.preventDefault();
                tagSuggestions.style.display = 'none';
                break;
                
            case 'Enter':
                if (document.activeElement !== tagsInput && document.activeElement.classList.contains('tag-suggestion')) {
                    e.preventDefault();
                    document.activeElement.click();
                }
                break;
        }
    });
}); 