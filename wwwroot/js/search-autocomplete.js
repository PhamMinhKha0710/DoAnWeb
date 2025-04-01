/**
 * Search Autocomplete - Handles search functionality in the navbar
 */
document.addEventListener('DOMContentLoaded', function() {
    // Elements
    const searchInput = document.getElementById('globalSearchInput');
    const searchContainer = document.getElementById('globalSearchContainer');
    const searchDropdown = document.getElementById('searchSuggestionsDropdown');
    
    if (!searchInput || !searchDropdown) return;
    
    let debounceTimer;
    let currentSearchTerm = '';
    
    // Show the dropdown
    function showDropdown() {
        searchDropdown.classList.add('active');
    }
    
    // Hide the dropdown
    function hideDropdown() {
        searchDropdown.classList.remove('active');
    }
    
    // Show loading indicator
    function showLoading() {
        const loadingIndicator = document.getElementById('searchLoadingIndicator');
        if (loadingIndicator) {
            loadingIndicator.classList.remove('d-none');
        }
        
        // Hide result sections while loading
        document.querySelectorAll('.search-result-section').forEach(el => {
            el.style.display = 'none';
        });
        
        // Hide no results message
        const noResultsMessage = document.getElementById('noResultsMessage');
        if (noResultsMessage) {
            noResultsMessage.classList.add('d-none');
        }
    }
    
    // Hide loading indicator
    function hideLoading() {
        const loadingIndicator = document.getElementById('searchLoadingIndicator');
        if (loadingIndicator) {
            loadingIndicator.classList.add('d-none');
        }
    }
    
    // Show no results message
    function showNoResults() {
        // Hide result sections
        document.querySelectorAll('.search-result-section').forEach(el => {
            el.style.display = 'none';
        });
        
        // Show no results message
        const noResultsMessage = document.getElementById('noResultsMessage');
        if (noResultsMessage) {
            noResultsMessage.classList.remove('d-none');
        }
    }
    
    // Highlight matched text
    function highlightText(text, query) {
        if (!text) return '';
        
        // Escape regex special characters in the query
        const escapedQuery = query.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
        const regex = new RegExp(`(${escapedQuery})`, 'gi');
        
        return text.replace(regex, '<span class="highlight">$1</span>');
    }
    
    // Search API function
    function performSearch(searchTerm) {
        if (!searchTerm || searchTerm.length < 2) {
            hideDropdown();
            return;
        }
        
        currentSearchTerm = searchTerm;
        showDropdown();
        showLoading();
        
        // Fetch results from the API
        fetch(`/api/search?q=${encodeURIComponent(searchTerm)}`)
            .then(response => {
                if (!response.ok) {
                    throw new Error('Search API request failed');
                }
                return response.json();
            })
            .then(data => {
                // Process results only if the search term hasn't changed
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
    
    // Display search results
    function displayResults(data, searchTerm) {
        hideLoading();
        
        const hasQuestions = data.questions && data.questions.length > 0;
        const hasTags = data.tags && data.tags.length > 0;
        const hasUsers = data.users && data.users.length > 0;
        const hasRepos = data.repositories && data.repositories.length > 0;
        
        // Show no results message if there are no results
        if (!hasQuestions && !hasTags && !hasUsers && !hasRepos) {
            showNoResults();
            return;
        }
        
        // Hide no results message
        const noResultsMessage = document.getElementById('noResultsMessage');
        if (noResultsMessage) {
            noResultsMessage.classList.add('d-none');
        }
        
        // Update questions section
        updateQuestionsSection(data.questions, searchTerm, hasQuestions);
        
        // Update tags section
        updateTagsSection(data.tags, searchTerm, hasTags);
        
        // Update users section
        updateUsersSection(data.users, searchTerm, hasUsers);
        
        // Update repositories section
        updateReposSection(data.repositories, searchTerm, hasRepos);
        
        // Update the view all button
        const viewAllButton = document.getElementById('viewAllSearchResults');
        if (viewAllButton) {
            viewAllButton.href = `/Questions/Index?search=${encodeURIComponent(searchTerm)}`;
        }
    }
    
    // Update questions section
    function updateQuestionsSection(questions, searchTerm, hasQuestions) {
        const questionSection = document.getElementById('questionResults');
        const questionList = document.getElementById('questionResultsList');
        
        if (!questionSection || !questionList) return;
        
        if (hasQuestions) {
            questionList.innerHTML = '';
            questions.forEach(question => {
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
    }
    
    // Update tags section
    function updateTagsSection(tags, searchTerm, hasTags) {
        const tagSection = document.getElementById('tagResults');
        const tagList = document.getElementById('tagResultsList');
        
        if (!tagSection || !tagList) return;
        
        if (hasTags) {
            tagList.innerHTML = '';
            const tagsContainer = document.createElement('div');
            tagsContainer.className = 'd-flex flex-wrap p-2';
            
            tags.forEach(tag => {
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
    }
    
    // Update users section
    function updateUsersSection(users, searchTerm, hasUsers) {
        const userSection = document.getElementById('userResults');
        const userList = document.getElementById('userResultsList');
        
        if (!userSection || !userList) return;
        
        if (hasUsers) {
            userList.innerHTML = '';
            users.forEach(user => {
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
    }
    
    // Update repositories section
    function updateReposSection(repositories, searchTerm, hasRepos) {
        const repoSection = document.getElementById('repoResults');
        const repoList = document.getElementById('repoResultsList');
        
        if (!repoSection || !repoList) return;
        
        if (hasRepos) {
            repoList.innerHTML = '';
            repositories.forEach(repo => {
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
    }
    
    // Input event handler with debounce
    searchInput.addEventListener('input', function(e) {
        const searchTerm = e.target.value.trim();
        
        // Clear any existing debounce timer
        if (debounceTimer) {
            clearTimeout(debounceTimer);
        }
        
        // Only search if there are at least 2 characters
        if (searchTerm.length >= 2) {
            // Set a new debounce timer
            debounceTimer = setTimeout(() => {
                performSearch(searchTerm);
            }, 300); // 300ms delay for debouncing
        } else if (searchTerm.length === 0) {
            hideDropdown();
        }
    });
    
    // Handle focus on the search input
    searchInput.addEventListener('focus', function() {
        const searchTerm = searchInput.value.trim();
        if (searchTerm.length >= 2) {
            performSearch(searchTerm);
        }
    });
    
    // Close dropdown when clicking outside
    document.addEventListener('click', function(e) {
        if (!searchContainer.contains(e.target) && searchDropdown.classList.contains('active')) {
            hideDropdown();
        }
    });
    
    // Close dropdown with Escape key
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape' && searchDropdown.classList.contains('active')) {
            hideDropdown();
            searchInput.blur();
        }
    });
    
    // Handle search form submission
    const searchForm = document.getElementById('globalSearchForm');
    if (searchForm) {
        searchForm.addEventListener('submit', function(e) {
            const searchTerm = searchInput.value.trim();
            
            // Prevent submission if search term is too short
            if (searchTerm.length < 2) {
                e.preventDefault();
                return false;
            }
            
            // Allow normal form submission (redirects to search results page)
            return true;
        });
    }
}); 