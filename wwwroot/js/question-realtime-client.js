/**
 * Question Real-time Client - Handles real-time question, answer and comment updates
 * Uses SignalR to provide real-time updates without page reload
 */
"use strict";

// Question Real-time Client module
const QuestionRealTimeClient = (function () {
    // Private variables
    let connection = null;
    let isConnected = false;
    let retryCount = 0;
    const maxRetries = 5;
    let questionId = 0;
    
    // Initialize the real-time system
    function init(currentQuestionId) {
        console.log("QuestionRealTimeClient: Initializing...");
        
        // Store question ID if we're on a question details page
        if (currentQuestionId && parseInt(currentQuestionId) > 0) {
            questionId = parseInt(currentQuestionId);
        }
        
        initializeSignalR();
        setupEventHandlers();
    }
    
    // Initialize SignalR connection
    function initializeSignalR() {
        try {
            console.log("QuestionRealTimeClient: Initializing SignalR...");
            
            // Check if SignalR is available
            if (typeof signalR === 'undefined') {
                console.error("SignalR library not loaded! Check if signalr.min.js is properly included.");
                setTimeout(initializeSignalR, 2000); // Retry after 2 seconds
                return;
            }
            
            // Create SignalR connection
            connection = new signalR.HubConnectionBuilder()
                .withUrl('/questionHub')
                .withAutomaticReconnect([0, 2000, 5000, 10000, 30000]) // Retry policy
                .configureLogging(signalR.LogLevel.Information)
                .build();
            
            // Set up connection event handlers
            connection.onreconnecting(error => {
                console.log('Reconnecting to question hub...', error);
                isConnected = false;
                showConnectionStatus('connecting');
            });
            
            connection.onreconnected(connectionId => {
                console.log('Reconnected to question hub:', connectionId);
                isConnected = true;
                showConnectionStatus('connected');
                
                // Re-join groups on reconnection
                if (questionId > 0) {
                    joinQuestionGroup(questionId);
                }
            });
            
            connection.onclose(error => {
                console.log('Disconnected from question hub:', error);
                isConnected = false;
                showConnectionStatus('disconnected');
                
                // Try to reconnect if not a deliberate close
                if (retryCount < maxRetries) {
                    startConnection();
                }
            });
            
            // Start the connection
            startConnection();
        } catch (error) {
            console.error("Error initializing SignalR:", error);
        }
    }
    
    // Start SignalR connection
    function startConnection() {
        if (connection) {
            console.log("QuestionRealTimeClient: Starting connection...");
            showConnectionStatus('connecting');
            
            connection.start()
                .then(() => {
                    console.log('Connected to question hub');
                    isConnected = true;
                    retryCount = 0;
                    showConnectionStatus('connected');
                    
                    // Join question-specific group if on a question details page
                    if (questionId > 0) {
                        joinQuestionGroup(questionId);
                    }
                })
                .catch(error => {
                    console.error('Error connecting to question hub:', error);
                    isConnected = false;
                    showConnectionStatus('disconnected');
                    
                    // Try to reconnect if max retries not reached
                    if (retryCount < maxRetries) {
                        retryCount++;
                        console.log(`Connection attempt ${retryCount} failed. Retrying in 5 seconds...`);
                        setTimeout(startConnection, 5000);
                    } else {
                        console.error(`Failed to connect after ${maxRetries} attempts`);
                    }
                });
        }
    }
    
    // Join a specific question group
    function joinQuestionGroup(id) {
        if (!isConnected || !connection) {
            console.warn(`Cannot join question group ${id}: Not connected`);
            return;
        }
        
        console.log(`Joining question group: ${id}`);
        connection.invoke('JoinQuestionGroup', id)
            .then(() => {
                console.log(`Successfully joined question group: ${id}`);
            })
            .catch(error => {
                console.error(`Error joining question group ${id}:`, error);
            });
    }
    
    // Show connection status indicator
    function showConnectionStatus(status) {
        let statusIndicator = document.getElementById('question-hub-status');
        
        if (!statusIndicator) {
            statusIndicator = document.createElement('div');
            statusIndicator.id = 'question-hub-status';
            statusIndicator.className = 'connection-status';
            document.body.appendChild(statusIndicator);
        }
        
        // Update status
        statusIndicator.className = `connection-status ${status}`;
        
        // Set text and icon based on status
        let statusText = '';
        let statusIcon = '';
        
        switch (status) {
            case 'connected':
                statusText = 'Connected';
                statusIcon = '<i class="bi bi-wifi"></i>';
                
                // Fade out connected status after 3 seconds
                setTimeout(() => {
                    statusIndicator.remove();
                }, 3000);
                break;
                
            case 'connecting':
                statusText = 'Connecting...';
                statusIcon = '<i class="bi bi-wifi-off"></i>';
                break;
                
            case 'disconnected':
                statusText = 'Disconnected';
                statusIcon = '<i class="bi bi-wifi-off"></i>';
                break;
        }
        
        statusIndicator.innerHTML = `${statusIcon} ${statusText}`;
    }
    
    // Register event handlers
    function setupEventHandlers() {
        if (!connection) return;
        
        // Handle new question
        connection.on('ReceiveNewQuestion', handleNewQuestion);
        
        // Handle new answer
        connection.on('ReceiveNewAnswer', handleNewAnswer);
        
        // Handle answer count update
        connection.on('ReceiveAnswerCountUpdate', handleAnswerCountUpdate);
        
        // Handle new comment
        connection.on('ReceiveNewComment', handleNewComment);
        
        // Handle question update
        connection.on('ReceiveQuestionUpdate', handleQuestionUpdate);
    }
    
    // Handle new question
    function handleNewQuestion(question) {
        console.log('New question received:', question);
        
        // Only process if we're on a page that displays questions list
        const questionsContainer = document.querySelector('.questions-list');
        if (!questionsContainer) return;
        
        // Create HTML for the new question
        const questionHtml = createQuestionHtml(question);
        
        // Prepend to questions list (assuming newest first)
        if (questionsContainer.firstChild) {
            const tempDiv = document.createElement('div');
            tempDiv.innerHTML = questionHtml;
            const questionElement = tempDiv.firstElementChild;
            
            // Add highlight animation class
            questionElement.classList.add('highlight-new');
            
            // Insert at the beginning
            questionsContainer.insertBefore(questionElement, questionsContainer.firstChild);
            
            // Scroll to the new question
            questionElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
        } else {
            questionsContainer.innerHTML = questionHtml;
        }
        
        // Update questions count if displayed
        updateQuestionsCount(1);
    }
    
    // Create HTML for a question
    function createQuestionHtml(question) {
        const date = new Date(question.createdDate);
        const formattedDate = date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
        
        // Format tags
        let tagsHtml = '';
        if (question.tags && question.tags.length > 0) {
            tagsHtml = '<div class="question-tags">';
            question.tags.forEach(tag => {
                tagsHtml += `<a href="/Questions?tag=${tag.name}" class="tag">${tag.name}</a>`;
            });
            tagsHtml += '</div>';
        }
        
        return `
            <div class="question-item" data-question-id="${question.questionId}">
                <div class="d-flex">
                    <div class="stats-container me-3">
                        <div class="stat-item">
                            <span class="stat-value">${question.score || 0}</span>
                            <span class="stat-label">votes</span>
                        </div>
                        <div class="stat-item">
                            <span class="stat-value">${question.answerCount || 0}</span>
                            <span class="stat-label">answers</span>
                        </div>
                        <div class="stat-item">
                            <span class="stat-value">${question.viewCount || 0}</span>
                            <span class="stat-label">views</span>
                        </div>
                    </div>
                    <div class="flex-grow-1">
                        <h5 class="question-title">
                            <a href="/Questions/Details/${question.questionId}">${question.title}</a>
                        </h5>
                        <div class="question-excerpt">
                            ${stripHtml(question.body).substring(0, 200)}...
                        </div>
                        ${tagsHtml}
                        <div class="question-meta">
                            <span>Asked ${formattedDate} by <a href="/Users/Profile/${question.userId}">${question.userName}</a></span>
                        </div>
                    </div>
                </div>
            </div>
        `;
    }
    
    // Handle new answer
    function handleNewAnswer(answer) {
        console.log('New answer received:', answer);
        
        // Check if we're on the specific question page
        if (!questionId || questionId !== answer.questionId) return;
        
        // Get answers container
        const answersContainer = document.querySelector('.answers-container');
        if (!answersContainer) return;
        
        // Create HTML for the new answer
        const answerHtml = createAnswerHtml(answer);
        
        // Add to answers list
        const tempDiv = document.createElement('div');
        tempDiv.innerHTML = answerHtml;
        const answerElement = tempDiv.firstElementChild;
        
        // Add highlight animation class
        answerElement.classList.add('highlight-new');
        
        // Append to answers container
        answersContainer.appendChild(answerElement);
        
        // Scroll to the new answer
        answerElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
        
        // Update answers count on the page
        updateAnswersCount(1);
    }
    
    // Create HTML for an answer
    function createAnswerHtml(answer) {
        const date = new Date(answer.createdDate);
        const formattedDate = date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
        
        let acceptedHtml = '';
        if (answer.isAccepted) {
            acceptedHtml = '<div class="accepted-badge"><i class="bi bi-check-circle"></i> Accepted</div>';
        }
        
        return `
            <div class="answer-item" id="answer-${answer.answerId}" data-answer-id="${answer.answerId}">
                <div class="d-flex">
                    <div class="vote-container me-3">
                        <div class="vote-count">${answer.score || 0}</div>
                        <div class="vote-label">votes</div>
                    </div>
                    <div class="flex-grow-1">
                        ${acceptedHtml}
                        <div class="answer-content">
                            ${answer.body}
                        </div>
                        <div class="answer-meta">
                            <span>Answered ${formattedDate} by <a href="/Users/Profile/${answer.userId}">${answer.userName}</a></span>
                        </div>
                        <div class="comments-container mt-3" id="comments-answer-${answer.answerId}">
                            <!-- Comments will be added here -->
                        </div>
                        <div class="comment-form-container mt-2">
                            <input type="text" class="form-control comment-input" placeholder="Add a comment..." data-target-type="Answer" data-target-id="${answer.answerId}">
                        </div>
                    </div>
                </div>
            </div>
        `;
    }
    
    // Handle answer count update
    function handleAnswerCountUpdate(data) {
        console.log('Answer count update received:', data);
        
        // Update answer count in questions list (if visible)
        const questionItem = document.querySelector(`.question-item[data-question-id="${data.questionId}"]`);
        if (questionItem) {
            const answerCountElement = questionItem.querySelector('.stat-item:nth-child(2) .stat-value');
            if (answerCountElement) {
                const currentCount = parseInt(answerCountElement.textContent || '0');
                answerCountElement.textContent = currentCount + data.newCount;
            }
        }
    }
    
    // Handle new comment
    function handleNewComment(comment) {
        console.log('New comment received:', comment);
        
        // Only process if we're on the right question page
        if (!questionId || questionId !== comment.questionId) return;
        
        // Determine which comments container to update
        let commentsContainer;
        if (comment.targetType === 'Question') {
            commentsContainer = document.querySelector(`#comments-question-${comment.targetId}`);
        } else if (comment.targetType === 'Answer') {
            commentsContainer = document.querySelector(`#comments-answer-${comment.targetId}`);
        }
        
        if (!commentsContainer) return;
        
        // Create HTML for the new comment
        const commentHtml = createCommentHtml(comment);
        
        // Add to comments list
        const tempDiv = document.createElement('div');
        tempDiv.innerHTML = commentHtml;
        const commentElement = tempDiv.firstElementChild;
        
        // Add highlight animation class
        commentElement.classList.add('highlight-new');
        
        // Append to comments container
        commentsContainer.appendChild(commentElement);
    }
    
    // Create HTML for a comment
    function createCommentHtml(comment) {
        const date = new Date(comment.createdDate);
        const formattedDate = date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
        
        return `
            <div class="comment-item" data-comment-id="${comment.commentId}">
                <div class="comment-content">${comment.body}</div>
                <div class="comment-meta">
                    <span>Commented ${formattedDate} by <a href="/Users/Profile/${comment.userId}">${comment.userName}</a></span>
                </div>
            </div>
        `;
    }
    
    // Handle question update
    function handleQuestionUpdate(question) {
        console.log('Question update received:', question);
        
        // Update question in list view (if visible)
        const questionItem = document.querySelector(`.question-item[data-question-id="${question.questionId}"]`);
        if (questionItem) {
            // Update title
            const titleElement = questionItem.querySelector('.question-title a');
            if (titleElement) titleElement.textContent = question.title;
            
            // Update excerpt
            const excerptElement = questionItem.querySelector('.question-excerpt');
            if (excerptElement) excerptElement.textContent = stripHtml(question.body).substring(0, 200) + '...';
            
            // Update stats
            const scoreElement = questionItem.querySelector('.stat-item:nth-child(1) .stat-value');
            if (scoreElement) scoreElement.textContent = question.score || 0;
            
            const viewsElement = questionItem.querySelector('.stat-item:nth-child(3) .stat-value');
            if (viewsElement) viewsElement.textContent = question.viewCount || 0;
            
            // Add highlight animation
            questionItem.classList.add('highlight-new');
        }
        
        // If on question details page, update the question content
        if (questionId && questionId === question.questionId) {
            // Update title
            const titleElement = document.querySelector('.question-title h1');
            if (titleElement) titleElement.textContent = question.title;
            
            // Update body content
            const bodyElement = document.querySelector('.question-content');
            if (bodyElement) bodyElement.innerHTML = question.body;
            
            // Update score
            const scoreElement = document.querySelector('.question-vote-count');
            if (scoreElement) scoreElement.textContent = question.score || 0;
        }
    }
    
    // Update questions count
    function updateQuestionsCount(increment) {
        const countElement = document.querySelector('.questions-count');
        if (countElement) {
            const currentCount = parseInt(countElement.textContent.match(/\d+/) || '0');
            countElement.textContent = `${currentCount + increment} Questions`;
        }
    }
    
    // Update answers count
    function updateAnswersCount(increment) {
        const countElement = document.querySelector('.answers-count');
        if (countElement) {
            const currentCount = parseInt(countElement.textContent.match(/\d+/) || '0');
            const newCount = currentCount + increment;
            countElement.textContent = `${newCount} Answer${newCount !== 1 ? 's' : ''}`;
        }
    }
    
    // Helper function to strip HTML tags
    function stripHtml(html) {
        const temp = document.createElement('div');
        temp.innerHTML = html;
        return temp.textContent || temp.innerText || '';
    }
    
    // Public API
    return {
        init: init,
        joinQuestionGroup: joinQuestionGroup
    };
})();

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    // Get current question ID from the page if available
    let currentQuestionId = null;
    const questionContainer = document.querySelector('[data-question-id]');
    if (questionContainer) {
        currentQuestionId = questionContainer.dataset.questionId;
    }
    
    // Initialize the client
    QuestionRealTimeClient.init(currentQuestionId);
    
    // Set up comment form handlers
    setupCommentForms();
});

// Set up comment form handlers
function setupCommentForms() {
    document.addEventListener('keydown', function(event) {
        const target = event.target;
        
        // Check if this is a comment input
        if (!target.classList.contains('comment-input')) return;
        
        // If Enter key is pressed
        if (event.key === 'Enter' && !event.shiftKey) {
            event.preventDefault();
            
            const comment = target.value.trim();
            if (comment === '') return;
            
            const targetType = target.dataset.targetType;
            const targetId = parseInt(target.dataset.targetId);
            
            // Send comment to server (via normal AJAX - the server will broadcast via SignalR)
            submitComment(comment, targetType, targetId)
                .then(() => {
                    // Clear the input field after successful submission
                    target.value = '';
                })
                .catch(error => {
                    console.error('Error submitting comment:', error);
                    alert('Failed to submit comment. Please try again.');
                });
        }
    });
}

// Submit a comment via AJAX
async function submitComment(text, targetType, targetId) {
    try {
        const response = await fetch('/Comments/Create', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                body: text,
                targetType: targetType,
                targetId: targetId
            })
        });
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        return await response.json();
    } catch (error) {
        console.error('Error submitting comment:', error);
        throw error;
    }
} 