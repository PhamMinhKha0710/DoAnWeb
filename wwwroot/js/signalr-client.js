/**
 * SignalR client for real-time communication
 */
const DevCommunitySignalR = {
    questionConnection: null,
    notificationConnection: null,
    
    /**
     * Initialize the SignalR connections
     */
    init: function() {
        console.log("DevCommunitySignalR: Initializing connections...");
        
        // Extra check to verify SignalR is available
        if (typeof signalR === 'undefined') {
            console.error("SignalR library not loaded! Check if signalr.min.js is properly included.");
            alert("Error: SignalR library not loaded. This will affect real-time features.");
            return;
        }
        
        // Initialize the hubs
        this.initQuestionHub();
        this.initNotificationHub();
        
        // Set a periodic check to ensure connections stay alive
        setInterval(() => {
            this.checkAndReconnect();
        }, 30000);
    },
    
    /**
     * Check hub connections and reconnect if needed
     */
    checkAndReconnect: function() {
        // Check QuestionHub
        if (this.questionConnection && 
            this.questionConnection.state !== signalR.HubConnectionState.Connected && 
            this.questionConnection.state !== signalR.HubConnectionState.Connecting &&
            this.questionConnection.state !== signalR.HubConnectionState.Reconnecting) {
            console.log("QuestionHub disconnected, attempting to reconnect...");
            this.startQuestionConnection();
        }
        
        // Check NotificationHub
        if (this.notificationConnection && 
            this.notificationConnection.state !== signalR.HubConnectionState.Connected && 
            this.notificationConnection.state !== signalR.HubConnectionState.Connecting &&
            this.notificationConnection.state !== signalR.HubConnectionState.Reconnecting) {
            console.log("NotificationHub disconnected, attempting to reconnect...");
            this.startNotificationConnection();
        }
    },
    
    /**
     * Helper to get auth token from cookies or storage
     */
    getAuthToken: function() {
        // Try to get from cookie
        const cookies = document.cookie.split(';');
        for (let i = 0; i < cookies.length; i++) {
            const cookie = cookies[i].trim();
            if (cookie.startsWith('.AspNetCore.Identity.Application=') || 
                cookie.startsWith('Authorization=') ||
                cookie.startsWith('DevCommunityAuth=')) {
                return cookie.split('=')[1];
            }
        }
        
        // Or from localStorage if your app uses that
        const token = localStorage.getItem('auth_token');
        if (token) return token;
        
        // Return null if no token found
        return null;
    },
    
    /**
     * Initialize the connection to QuestionHub
     */
    initQuestionHub: function() {
        try {
            console.log("DevCommunitySignalR: Initializing QuestionHub...");
            
            // Create the connection
            this.questionConnection = new signalR.HubConnectionBuilder()
                .withUrl("/questionHub")
                .withAutomaticReconnect([0, 2000, 5000, 10000, 30000]) // Retry policy
                .configureLogging(signalR.LogLevel.Information)
                .build();
                
            // Handle new questions being posted
            this.questionConnection.on("NewQuestionPosted", (question) => {
                console.log("New question posted:", question);
                
                // Handle on question list page
                if (document.querySelector('.question-list')) {
                    this.appendNewQuestion(question);
                }
            });
            
            // Handle new answers being posted
            this.questionConnection.on("NewAnswerPosted", (answer) => {
                console.log("New answer posted:", answer);
                
                // If we're on the question detail page for this question
                const questionDetailContainer = document.querySelector(`.question-detail[data-question-id="${answer.questionId}"]`);
                if (questionDetailContainer) {
                    this.appendNewAnswer(answer);
                }
            });
            
            // Handle new comments being posted
            this.questionConnection.on("ReceiveNewComment", (comment) => {
                console.log("New comment received:", comment);
                
                // If we're on the question detail page for this comment's question
                if (comment.questionId) {
                    const questionDetailContainer = document.querySelector(`.question-detail[data-question-id="${comment.questionId}"]`);
                    if (questionDetailContainer) {
                        this.appendNewComment(comment);
                    }
                }
            });
            
            // Handle question updates
            this.questionConnection.on("QuestionUpdated", (question) => {
                console.log("Question updated:", question);
                
                // If we're on the question detail page for this question
                const questionDetailContainer = document.querySelector(`.question-detail[data-question-id="${question.questionId}"]`);
                if (questionDetailContainer) {
                    this.updateQuestionDetails(question);
                }
            });
            
            // Start the connection
            this.startQuestionConnection();
        } catch (error) {
            console.error("Error in initQuestionHub:", error);
        }
    },
    
    /**
     * Start QuestionHub connection with retry mechanism
     */
    startQuestionConnection: function(retryAttempt = 0) {
        if (retryAttempt > 5) {
            console.error("Failed to connect to QuestionHub after multiple attempts");
            return;
        }
        
        console.log(`Attempting to connect to QuestionHub (attempt ${retryAttempt + 1})...`);
        
        this.questionConnection.start()
            .then(() => {
                console.log("Connected to QuestionHub successfully");
                
                // Join specific question group if we're on a question page
                const questionDetailContainer = document.querySelector('.question-detail');
                if (questionDetailContainer) {
                    const questionId = questionDetailContainer.getAttribute('data-question-id');
                    if (questionId) {
                        this.joinQuestionGroup(parseInt(questionId));
                    }
                }
            })
            .catch(err => {
                console.error("Error connecting to QuestionHub:", err);
                
                // Retry with exponential backoff
                const retryDelay = Math.min(1000 * Math.pow(2, retryAttempt), 30000);
                console.log(`Retrying in ${retryDelay}ms...`);
                
                setTimeout(() => {
                    this.startQuestionConnection(retryAttempt + 1);
                }, retryDelay);
            });
    },
    
    /**
     * Initialize the connection to NotificationHub
     */
    initNotificationHub: function() {
        try {
            console.log("DevCommunitySignalR: Initializing NotificationHub...");
            
            // Check if user is logged in by looking for auth-related elements
            const isLoggedIn = document.querySelector('.nav-item.dropdown:has(.dropdown-item[href="javascript:void(0);"][onclick*="logoutForm"])') !== null || 
                             document.querySelector('form#logoutForm') !== null;
            
            if (!isLoggedIn) {
                console.log("User not logged in. NotificationHub requires authentication.");
                return; // Don't attempt to connect if not logged in
            }
            
            // Create the connection with authentication
            this.notificationConnection = new signalR.HubConnectionBuilder()
                .withUrl("/notificationHub", {
                    // Pass authentication tokens if available
                    accessTokenFactory: () => {
                        // Try to get token from cookies or local storage if your app stores it there
                        const token = this.getAuthToken();
                        return token;
                    }
                })
                .withAutomaticReconnect([0, 2000, 5000, 10000, 30000]) // Retry policy
                .configureLogging(signalR.LogLevel.Information)
                .build();
                
            // Handle notifications
            this.notificationConnection.on("ReceiveNotification", (notification) => {
                console.log("Received notification:", notification);
                this.showNotification(notification);
            });
            
            // Start the connection
            this.startNotificationConnection();
        } catch (error) {
            console.error("Error in initNotificationHub:", error);
        }
    },
    
    /**
     * Start NotificationHub connection with retry mechanism
     */
    startNotificationConnection: function(retryAttempt = 0) {
        if (retryAttempt > 5) {
            console.error("Failed to connect to NotificationHub after multiple attempts");
            return;
        }
        
        console.log(`Attempting to connect to NotificationHub (attempt ${retryAttempt + 1})...`);
        
        this.notificationConnection.start()
            .then(() => {
                console.log("Connected to NotificationHub successfully");
            })
            .catch(err => {
                console.error("Error connecting to NotificationHub:", err);
                
                // Retry with exponential backoff
                const retryDelay = Math.min(1000 * Math.pow(2, retryAttempt), 30000);
                console.log(`Retrying in ${retryDelay}ms...`);
                
                setTimeout(() => {
                    this.startNotificationConnection(retryAttempt + 1);
                }, retryDelay);
            });
    },
    
    /**
     * Join a specific question group to receive updates about answers
     */
    joinQuestionGroup: function(questionId) {
        if (!this.questionConnection) {
            console.error("QuestionConnection is not initialized");
            return;
        }
        
        if (this.questionConnection.state === signalR.HubConnectionState.Connected) {
            this.questionConnection.invoke("JoinQuestionGroup", questionId)
                .then(() => {
                    console.log(`Joined question group: ${questionId}`);
                })
                .catch(err => {
                    console.error(`Error joining question group ${questionId}:`, err);
                });
        } else {
            console.warn(`Cannot join question group ${questionId}: Connection not in Connected state`);
            // Store for later once connected
            this._pendingQuestionGroup = questionId;
            
            // Try to connect if disconnected
            if (this.questionConnection.state === signalR.HubConnectionState.Disconnected) {
                this.startQuestionConnection();
            }
        }
    },
    
    /**
     * Leave a specific question group
     */
    leaveQuestionGroup: function(questionId) {
        if (this.questionConnection && this.questionConnection.state === signalR.HubConnectionState.Connected) {
            this.questionConnection.invoke("LeaveQuestionGroup", questionId)
                .catch(err => {
                    console.error(`Error leaving question group ${questionId}:`, err);
                });
        }
    },
    
    /**
     * Append a new question to the question list
     */
    appendNewQuestion: function(question) {
        const questionList = document.querySelector('.question-list');
        if (!questionList) return;
        
        // Create question HTML
        const formattedDate = new Date(question.createdDate).toLocaleString();
        const questionHtml = `
            <div class="question-item new-question" data-question-id="${question.questionId}">
                <div class="question-header">
                    <h3><a href="/Question/Details/${question.questionId}">${question.title}</a></h3>
                    <span class="question-meta">Posted by ${question.username} on ${formattedDate}</span>
                </div>
                <div class="question-tags">
                    <span class="tag-count">${question.tagCount} tags</span>
                </div>
            </div>
        `;
        
        // Add to the top of the list with a highlight effect
        questionList.insertAdjacentHTML('afterbegin', questionHtml);
        
        // Apply highlight animation
        setTimeout(() => {
            const newQuestion = document.querySelector('.new-question');
            if (newQuestion) {
                newQuestion.classList.add('highlight-new');
                
                // Remove highlight after animation
                setTimeout(() => {
                    newQuestion.classList.remove('new-question', 'highlight-new');
                }, 3000);
            }
        }, 100);
    },
    
    /**
     * Append a new answer to the answers list
     */
    appendNewAnswer: function(answer) {
        const answersList = document.querySelector('.answers-list');
        if (!answersList) return;
        
        // Create answer HTML
        const formattedDate = new Date(answer.createdDate).toLocaleString();
        const answerHtml = `
            <div class="answer-item new-answer" data-answer-id="${answer.answerId}">
                <div class="answer-header">
                    <span class="answer-meta">Answered by ${answer.username} on ${formattedDate}</span>
                    ${answer.isAccepted ? '<span class="accepted-badge">✓ Accepted</span>' : ''}
                </div>
                <div class="answer-content">
                    ${answer.content}
                </div>
            </div>
        `;
        
        // Add to the answers list with a highlight effect
        answersList.insertAdjacentHTML('beforeend', answerHtml);
        
        // Apply highlight animation
        setTimeout(() => {
            const newAnswer = document.querySelector('.new-answer');
            if (newAnswer) {
                newAnswer.classList.add('highlight-new');
                
                // Remove highlight after animation
                setTimeout(() => {
                    newAnswer.classList.remove('new-answer', 'highlight-new');
                }, 3000);
            }
        }, 100);
        
        // Update answer count
        const answerCount = document.querySelector('.answer-count');
        if (answerCount) {
            const currentCount = parseInt(answerCount.textContent) || 0;
            answerCount.textContent = currentCount + 1;
        }
    },
    
    /**
     * Update question details when question is edited
     */
    updateQuestionDetails: function(question) {
        // Update title
        const titleElement = document.querySelector('.question-title');
        if (titleElement) {
            titleElement.textContent = question.title;
        }
        
        // Update content
        const contentElement = document.querySelector('.question-content');
        if (contentElement) {
            contentElement.innerHTML = question.content;
        }
        
        // Update resolved status
        if (question.isResolved) {
            const statusElement = document.querySelector('.question-status');
            if (statusElement) {
                statusElement.innerHTML = '<span class="resolved-badge">✓ Resolved</span>';
            }
        }
    },
    
    /**
     * Show a notification to the user
     */
    showNotification: function(notification) {
        // Create notification element
        const notifContainer = document.querySelector('.notification-container') || 
            document.createElement('div');
        
        if (!document.querySelector('.notification-container')) {
            notifContainer.className = 'notification-container';
            notifContainer.style.cssText = 'position: fixed; bottom: 20px; right: 20px; z-index: 1050;';
            document.body.appendChild(notifContainer);
        }
        
        // Create notification toast
        const notifId = `notification-${Date.now()}`;
        const notifEl = document.createElement('div');
        notifEl.id = notifId;
        notifEl.className = 'notification-toast';
        notifEl.style.cssText = `
            background-color: #fff;
            border-left: 4px solid #007bff;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
            border-radius: 4px;
            padding: 15px;
            margin-bottom: 10px;
            max-width: 350px;
            animation: slideIn 0.3s ease-out forwards;
        `;
        
        // Icon based on notification type
        let iconClass = 'bi-bell';
        if (notification.type === 'Answer') iconClass = 'bi-chat-left-text';
        if (notification.type === 'Comment') iconClass = 'bi-chat-dots';
        if (notification.type === 'Vote') iconClass = 'bi-hand-thumbs-up';
        
        // Notification content
        notifEl.innerHTML = `
            <div style="display: flex; align-items: flex-start;">
                <div style="margin-right: 10px;">
                    <i class="bi ${iconClass}" style="font-size: 20px; color: #007bff;"></i>
                </div>
                <div style="flex: 1;">
                    <div style="font-weight: 600; margin-bottom: 5px;">${notification.title}</div>
                    <div style="font-size: 14px; margin-bottom: 10px;">${notification.message}</div>
                    ${notification.url ? `<a href="${notification.url}" class="btn btn-sm btn-primary">View</a>` : ''}
                </div>
                <div>
                    <button class="btn-close" style="font-size: 12px;" onclick="this.parentNode.parentNode.parentNode.remove()"></button>
                </div>
            </div>
        `;
        
        // Add to container
        notifContainer.appendChild(notifEl);
        
        // Auto-remove after 8 seconds
        setTimeout(() => {
            const notification = document.getElementById(notifId);
            if (notification) {
                notification.style.animation = 'slideOut 0.3s ease-in forwards';
                setTimeout(() => notification.remove(), 300);
            }
        }, 8000);
    },
    
    /**
     * Append a new comment to the appropriate container
     */
    appendNewComment: function(comment) {
        console.log("Appending new comment:", comment);
        
        let container;
        if (comment.targetType === "Question") {
            // Comment on a question
            container = document.querySelector(`.question-detail[data-question-id="${comment.questionId}"] .question-comments-container`);
        } else if (comment.targetType === "Answer") {
            // Comment on an answer
            container = document.querySelector(`.answer-item[data-answer-id="${comment.targetId}"] .answer-comments-container`);
        }
        
        if (!container) {
            console.error(`Container not found for comment: ${comment.commentId}, type: ${comment.targetType}, id: ${comment.targetId}`);
            return;
        }
        
        // Check if comment already exists
        if (document.querySelector(`#comment-${comment.commentId}`)) {
            console.log(`Comment ${comment.commentId} already exists, not adding duplicate`);
            return;
        }
        
        // Create the comment element
        const commentHtml = this.createCommentHtml(comment);
        
        // Check if this is the first comment
        const noCommentsMsg = container.querySelector('.no-comments-message');
        if (noCommentsMsg) {
            noCommentsMsg.remove();
        }
        
        // Add the new comment
        container.insertAdjacentHTML('beforeend', commentHtml);
        
        // Highlight the new comment
        const newComment = container.querySelector(`#comment-${comment.commentId}`);
        if (newComment) {
            newComment.classList.add('highlight-new');
            
            // Update comment count
            const countElement = container.parentElement.querySelector('.comment-count-badge');
            if (countElement) {
                const currentCount = parseInt(countElement.textContent);
                countElement.textContent = isNaN(currentCount) ? '1' : (currentCount + 1).toString();
            }
        }
    },
    
    /**
     * Create HTML for a comment
     */
    createCommentHtml: function(comment) {
        const createdDate = new Date(comment.createdDate).toLocaleString();
        const avatarUrl = comment.userAvatar || '/images/default-avatar.png';
        
        return `
            <div id="comment-${comment.commentId}" class="comment-item card mb-2">
                <div class="card-body">
                    <div class="d-flex">
                        <div class="comment-avatar me-2">
                            <img src="${avatarUrl}" class="rounded-circle" alt="${comment.userName}">
                        </div>
                        <div class="comment-content flex-grow-1">
                            <div class="comment-text">${comment.body}</div>
                            <div class="comment-meta text-muted">
                                <span class="comment-author">${comment.userName}</span>
                                <span class="comment-date">${createdDate}</span>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        `;
    },
    
    /**
     * Submit a comment to the server
     */
    submitComment: function(text, targetType, targetId, callback) {
        if (!text || !targetType || !targetId) {
            console.error("Missing required parameter for comment submission");
            if (callback) callback(false, "Missing required parameters");
            return;
        }
        
        // Create comment data
        const commentData = {
            body: text,
            targetType: targetType,
            targetId: parseInt(targetId)
        };
        
        // Post to server
        fetch('/Comments/Create', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': this.getAntiForgeryToken()
            },
            body: JSON.stringify(commentData)
        })
        .then(response => response.json())
        .then(result => {
            if (result.success) {
                console.log("Comment submitted successfully:", result);
                if (callback) callback(true, result);
            } else {
                console.error("Error submitting comment:", result.message);
                if (callback) callback(false, result.message);
            }
        })
        .catch(error => {
            console.error("Error submitting comment:", error);
            if (callback) callback(false, "Network error occurred");
        });
    },
    
    /**
     * Get anti-forgery token from the page
     */
    getAntiForgeryToken: function() {
        const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenElement ? tokenElement.value : '';
    }
};

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    try {
        // Add a longer delay to ensure all resources are loaded
        setTimeout(function() {
            // Check for SignalR library first
            if (typeof signalR === 'undefined') {
                console.error("SignalR is not loaded. Will retry in 2 seconds...");
                
                // Retry after a delay to allow for potential async loading
                setTimeout(function() {
                    if (typeof signalR !== 'undefined') {
                        console.log("SignalR now available, initializing...");
                        DevCommunitySignalR.init();
                    } else {
                        console.error("SignalR still not loaded after delay. Real-time features won't work.");
                        // One more final attempt
                        setTimeout(function() {
                            if (typeof signalR !== 'undefined') {
                                console.log("SignalR available on final attempt, initializing...");
                                DevCommunitySignalR.init();
                            }
                        }, 2000);
                    }
                }, 2000);
            } else {
                // SignalR is available, initialize now
                console.log("SignalR available, initializing right away...");
                DevCommunitySignalR.init();
            }
        }, 1000);
    } catch (error) {
        console.error("Error initializing SignalR clients:", error);
    }
}); 