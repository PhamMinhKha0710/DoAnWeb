// JavaScript for handling saved items functionality

document.addEventListener('DOMContentLoaded', function() {
    // Initialize save buttons for questions and answers
    initializeSaveButtons();
});

/**
 * Initialize save buttons for questions and answers
 */
function initializeSaveButtons() {
    // Handle question save button clicks
    const saveQuestionButtons = document.querySelectorAll('.save-question-btn');
    saveQuestionButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            e.preventDefault();
            const questionId = this.getAttribute('data-question-id');
            toggleSaveQuestion(questionId, this);
        });
    });

    // Handle answer save button clicks
    const saveAnswerButtons = document.querySelectorAll('.save-answer-btn');
    saveAnswerButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            e.preventDefault();
            const answerId = this.getAttribute('data-answer-id');
            toggleSaveAnswer(answerId, this);
        });
    });
}

/**
 * Toggle save/unsave for a question
 * @param {number} questionId - The ID of the question to save/unsave
 * @param {HTMLElement} button - The button element that was clicked
 */
function toggleSaveQuestion(questionId, button) {
    const isSaved = button.classList.contains('saved');
    const url = isSaved ? `/SavedItems/RemoveSavedItem?itemType=Question&itemId=${questionId}` : `/SavedItems/SaveQuestion/${questionId}`;
    
    fetch(url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-Requested-With': 'XMLHttpRequest',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
        }
    })
    .then(response => {
        if (response.ok) {
            return response.json();
        }
        throw new Error('Network response was not ok.');
    })
    .then(data => {
        if (data.success) {
            // Toggle saved state
            button.classList.toggle('saved');
            
            // Update icon and tooltip
            const icon = button.querySelector('i');
            if (button.classList.contains('saved')) {
                icon.classList.remove('bi-bookmark');
                icon.classList.add('bi-bookmark-fill');
                button.setAttribute('title', 'Unsave this question');
                showToast('Question saved successfully!');
            } else {
                icon.classList.remove('bi-bookmark-fill');
                icon.classList.add('bi-bookmark');
                button.setAttribute('title', 'Save this question');
                showToast('Question removed from saved items.');
            }
        } else {
            showToast('Error: ' + (data.message || 'Could not save/unsave question.'));
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showToast('Error: Could not save/unsave question.');
    });
}

/**
 * Toggle save/unsave for an answer
 * @param {number} answerId - The ID of the answer to save/unsave
 * @param {HTMLElement} button - The button element that was clicked
 */
function toggleSaveAnswer(answerId, button) {
    const isSaved = button.classList.contains('saved');
    const url = isSaved ? `/SavedItems/RemoveSavedItem?itemType=Answer&itemId=${answerId}` : `/SavedItems/SaveAnswer/${answerId}`;
    
    fetch(url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-Requested-With': 'XMLHttpRequest',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
        }
    })
    .then(response => {
        if (response.ok) {
            return response.json();
        }
        throw new Error('Network response was not ok.');
    })
    .then(data => {
        if (data.success) {
            // Toggle saved state
            button.classList.toggle('saved');
            
            // Update icon and tooltip
            const icon = button.querySelector('i');
            if (button.classList.contains('saved')) {
                icon.classList.remove('bi-bookmark');
                icon.classList.add('bi-bookmark-fill');
                button.setAttribute('title', 'Unsave this answer');
                showToast('Answer saved successfully!');
            } else {
                icon.classList.remove('bi-bookmark-fill');
                icon.classList.add('bi-bookmark');
                button.setAttribute('title', 'Save this answer');
                showToast('Answer removed from saved items.');
            }
        } else {
            showToast('Error: ' + (data.message || 'Could not save/unsave answer.'));
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showToast('Error: Could not save/unsave answer.');
    });
}

/**
 * Show a toast notification
 * @param {string} message - The message to display
 */
function showToast(message) {
    // Check if toast container exists, if not create it
    let toastContainer = document.getElementById('toast-container');
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.id = 'toast-container';
        toastContainer.className = 'position-fixed bottom-0 end-0 p-3';
        document.body.appendChild(toastContainer);
    }
    
    // Create toast element
    const toastId = 'toast-' + Date.now();
    const toastHtml = `
        <div id="${toastId}" class="toast" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="toast-header">
                <i class="bi bi-info-circle me-2"></i>
                <strong class="me-auto">Notification</strong>
                <button type="button" class="btn-close" data-bs-dismiss="toast" aria-label="Close"></button>
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
    const toast = new bootstrap.Toast(toastElement, { delay: 3000 });
    toast.show();
    
    // Remove toast after it's hidden
    toastElement.addEventListener('hidden.bs.toast', function() {
        toastElement.remove();
    });
}