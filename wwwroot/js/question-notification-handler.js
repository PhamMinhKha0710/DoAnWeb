/**
 * Question Notification Handler
 * Handles joining notification groups for real-time updates when viewing questions
 */

document.addEventListener('DOMContentLoaded', function() {
    // Check if we're on a question details page
    const questionDetailContainer = document.querySelector('.question-detail-card');
    if (!questionDetailContainer) return;
    
    // Get question ID from the container
    const questionId = questionDetailContainer.closest('[data-question-id]')?.getAttribute('data-question-id') || 
                      new URLSearchParams(window.location.search).get('id');
    
    if (!questionId) {
        console.warn('Question ID not found, cannot join notification group');
        return;
    }
    
    console.log(`Question page detected, question ID: ${questionId}`);
    
    // Wait for NotificationHandler to initialize
    const joinQuestionGroup = function() {
        // Check if NotificationHandler is available
        if (typeof NotificationHandler !== 'undefined' && 
            typeof NotificationHandler.joinGroup === 'function') {
            
            // Join the question notification group
            const groupName = `question-${questionId}`;
            console.log(`Joining notification group: ${groupName}`);
            NotificationHandler.joinGroup(groupName);
            
            // Also join the question group in QuestionHub if DevCommunitySignalR is available
            if (typeof DevCommunitySignalR !== 'undefined' && 
                typeof DevCommunitySignalR.joinQuestionGroup === 'function') {
                DevCommunitySignalR.joinQuestionGroup(parseInt(questionId));
            }
        } else {
            // Retry after a short delay
            setTimeout(joinQuestionGroup, 1000);
        }
    };
    
    // Start trying to join the group
    joinQuestionGroup();
    
    // Handle page unload - leave the group when navigating away
    window.addEventListener('beforeunload', function() {
        if (typeof NotificationHandler !== 'undefined' && 
            typeof NotificationHandler.leaveGroup === 'function') {
            
            const groupName = `question-${questionId}`;
            NotificationHandler.leaveGroup(groupName);
        }
        
        if (typeof DevCommunitySignalR !== 'undefined' && 
            typeof DevCommunitySignalR.leaveQuestionGroup === 'function') {
            DevCommunitySignalR.leaveQuestionGroup(parseInt(questionId));
        }
    });
});