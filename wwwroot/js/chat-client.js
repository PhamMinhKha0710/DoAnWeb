/**
 * Chat Client - Handles real-time messaging functionality
 */
const ChatManager = (function() {
    // Private variables
    let connection = null;
    let activeConversationId = null;
    let isTyping = false;
    let typingTimeout = null;
    let retryCount = 0;
    const maxRetries = 5;

    // Initialize the chat system
    function init() {
        console.log("ChatManager: Initializing...");
        
        // Only initialize if user is logged in
        if (isUserLoggedIn()) {
            initializeSignalR();
            setupChatUI();
        } else {
            console.log("ChatManager: User not logged in, chat won't work");
        }
    }
    
    // Check if user is logged in
    function isUserLoggedIn() {
        return document.querySelector('.nav-item.dropdown:has(.dropdown-item[href="javascript:void(0);"][onclick*="logoutForm"])') !== null || 
               document.querySelector('form#logoutForm') !== null;
    }
    
    // Initialize SignalR connection
    function initializeSignalR() {
        try {
            console.log("ChatManager: Initializing SignalR...");
            
            // Check if SignalR is available
            if (typeof signalR === 'undefined') {
                console.error("SignalR library not loaded! Chat won't work.");
                setTimeout(initializeSignalR, 2000); // Retry after 2 seconds
                return;
            }
            
            // Create SignalR connection with authentication
            connection = new signalR.HubConnectionBuilder()
                .withUrl('/chatHub')
                .withAutomaticReconnect([0, 2000, 5000, 10000, 30000]) // Retry policy
                .configureLogging(signalR.LogLevel.Information)
                .build();
            
            // Set up connection event handlers
            connection.onreconnecting(error => {
                console.log('Reconnecting to chat hub...', error);
                updateConnectionStatus('connecting');
            });
            
            connection.onreconnected(connectionId => {
                console.log('Reconnected to chat hub:', connectionId);
                retryCount = 0;
                updateConnectionStatus('connected');
                
                // Rejoin active conversation if any
                if (activeConversationId) {
                    joinConversation(activeConversationId);
                }
            });
            
            connection.onclose(error => {
                console.log('Chat connection closed:', error);
                updateConnectionStatus('disconnected');
                
                // Try to reconnect if max retries not reached
                if (retryCount < maxRetries) {
                    retryCount++;
                    setTimeout(startConnection, 5000);
                }
            });
            
            // Set up message handlers
            connection.on('ReceiveMessage', handleNewMessage);
            connection.on('MessageRead', handleMessageRead);
            connection.on('UserIsTyping', handleUserTyping);
            connection.on('NewChatMessage', handleNewChatNotification);
            
            // Start the connection
            startConnection();
        } catch (error) {
            console.error('Error initializing SignalR for chat:', error);
        }
    }
    
    // Set up chat UI event listeners
    function setupChatUI() {
        // Listen for selecting a conversation
        document.addEventListener('click', function(e) {
            // If user clicked on a conversation item
            if (e.target.closest('.conversation-item')) {
                const conversationItem = e.target.closest('.conversation-item');
                const conversationId = conversationItem.getAttribute('data-conversation-id');
                
                if (conversationId) {
                    // Set as active conversation
                    setActiveConversation(parseInt(conversationId));
                    
                    // Update UI
                    document.querySelectorAll('.conversation-item').forEach(item => {
                        item.classList.remove('active');
                    });
                    conversationItem.classList.add('active');
                    
                    // Load conversation messages
                    loadConversation(parseInt(conversationId));
                }
            }
        });
        
        // Listen for send message button clicks
        document.addEventListener('click', function(e) {
            if (e.target.matches('#send-message-btn')) {
                sendMessage();
            }
        });
        
        // Listen for Enter key in message input
        const messageInput = document.getElementById('message-input');
        if (messageInput) {
            messageInput.addEventListener('keypress', function(e) {
                if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    sendMessage();
                }
                
                // Send typing indicator
                handleLocalUserTyping();
            });
            
            // Also track input events for typing indicator
            messageInput.addEventListener('input', function() {
                handleLocalUserTyping();
            });
        }
    }
    
    // Update connection status display
    function updateConnectionStatus(status) {
        const statusElements = document.querySelectorAll('.chat-status-indicator');
        if (statusElements.length === 0) return;
        
        statusElements.forEach(element => {
            element.className = `chat-status-indicator ${status}`;
            
            let statusText = '';
            switch (status) {
                case 'connected':
                    statusText = 'Connected';
                    break;
                case 'connecting':
                    statusText = 'Connecting...';
                    break;
                case 'disconnected':
                    statusText = 'Disconnected';
                    break;
            }
            
            element.setAttribute('title', `Chat: ${statusText}`);
        });
    }
    
    // Start SignalR connection
    function startConnection() {
        if (connection) {
            console.log("ChatManager: Starting connection...");
            updateConnectionStatus('connecting');
            
            connection.start()
                .then(() => {
                    console.log('Connected to chat hub');
                    retryCount = 0;
                    updateConnectionStatus('connected');
                    
                    // If there's an active conversation, join it
                    if (activeConversationId) {
                        joinConversation(activeConversationId);
                    }
                })
                .catch(error => {
                    console.error('Error connecting to chat hub:', error);
                    updateConnectionStatus('disconnected');
                    
                    // Try to reconnect if max retries not reached
                    if (retryCount < maxRetries) {
                        retryCount++;
                        console.log(`Connection attempt ${retryCount} failed. Retrying in 5 seconds...`);
                        setTimeout(startConnection, 5000);
                    }
                });
        }
    }
    
    // Set active conversation
    function setActiveConversation(conversationId) {
        activeConversationId = conversationId;
        
        // Join the conversation on the hub
        if (connection && connection.state === signalR.HubConnectionState.Connected) {
            joinConversation(conversationId);
        }
    }
    
    // Join a conversation
    function joinConversation(conversationId) {
        if (connection && connection.state === signalR.HubConnectionState.Connected) {
            console.log(`Joining conversation ${conversationId}`);
            
            connection.invoke('JoinConversation', conversationId)
                .catch(error => {
                    console.error(`Error joining conversation ${conversationId}:`, error);
                });
        }
    }
    
    // Load conversation messages from server
    function loadConversation(conversationId) {
        const messagesContainer = document.querySelector('.chat-messages');
        if (!messagesContainer) return;
        
        // Show loading state
        messagesContainer.innerHTML = '<div class="loading-messages">Loading messages...</div>';
        
        // Fetch messages from server
        fetch(`/Chat/GetMessages?conversationId=${conversationId}`)
            .then(response => response.json())
            .then(messages => {
                displayMessages(messages);
            })
            .catch(error => {
                console.error('Error loading messages:', error);
                messagesContainer.innerHTML = '<div class="error-message">Failed to load messages. Please try again.</div>';
            });
    }
    
    // Display messages in the UI
    function displayMessages(messages) {
        const messagesContainer = document.querySelector('.chat-messages');
        if (!messagesContainer) return;
        
        if (messages.length === 0) {
            messagesContainer.innerHTML = '<div class="no-messages">No messages yet. Start the conversation!</div>';
            return;
        }
        
        // Clear container
        messagesContainer.innerHTML = '';
        
        // Get current user ID
        const currentUserId = getCurrentUserId();
        
        // Add each message
        messages.forEach(message => {
            const isOwn = message.senderId.toString() === currentUserId;
            const messageElement = createMessageElement(message, isOwn);
            messagesContainer.appendChild(messageElement);
        });
        
        // Scroll to bottom
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
    }
    
    // Create message element
    function createMessageElement(message, isOwn) {
        const messageDiv = document.createElement('div');
        messageDiv.className = `message ${isOwn ? 'message-own' : 'message-other'}`;
        messageDiv.setAttribute('data-message-id', message.messageId);
        
        // Format date
        const messageDate = new Date(message.sentAt);
        const formattedTime = messageDate.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        
        // Create message content
        messageDiv.innerHTML = `
            <div class="message-content">
                ${message.content}
            </div>
            <div class="message-meta">
                <span class="message-time">${formattedTime}</span>
                ${isOwn ? `<span class="message-status ${message.isRead ? 'read' : 'sent'}">
                    ${message.isRead ? 'Read' : 'Sent'}
                </span>` : ''}
            </div>
        `;
        
        return messageDiv;
    }
    
    // Get current user ID
    function getCurrentUserId() {
        const userIdElement = document.getElementById('current-user-id');
        return userIdElement ? userIdElement.value : null;
    }
    
    // Send a message
    function sendMessage() {
        if (!activeConversationId) {
            console.error('No active conversation');
            return;
        }
        
        const messageInput = document.getElementById('message-input');
        if (!messageInput) return;
        
        const content = messageInput.value.trim();
        if (!content) return;
        
        if (connection && connection.state === signalR.HubConnectionState.Connected) {
            connection.invoke('SendMessage', activeConversationId, content)
                .then(() => {
                    // Clear input
                    messageInput.value = '';
                    messageInput.focus();
                })
                .catch(error => {
                    console.error('Error sending message:', error);
                    alert('Failed to send message. Please try again.');
                });
        } else {
            alert('Not connected to chat. Please try again later.');
        }
    }
    
    // Handle new message received
    function handleNewMessage(message) {
        console.log('New message received:', message);
        
        // If this is for the active conversation, add it to the UI
        if (activeConversationId === message.conversationId) {
            const messagesContainer = document.querySelector('.chat-messages');
            if (!messagesContainer) return;
            
            const currentUserId = getCurrentUserId();
            const isOwn = message.senderId.toString() === currentUserId;
            
            const messageElement = createMessageElement(message, isOwn);
            messagesContainer.appendChild(messageElement);
            
            // Scroll to bottom
            messagesContainer.scrollTop = messagesContainer.scrollHeight;
            
            // If not our own message, mark as read
            if (!isOwn) {
                markMessageAsRead(message.messageId);
            }
        }
        
        // If not in the active conversation, show a notification
        else {
            // Update conversation list with new message indicator
            const conversationItem = document.querySelector(`.conversation-item[data-conversation-id="${message.conversationId}"]`);
            if (conversationItem) {
                conversationItem.classList.add('has-new-message');
                
                // Update last message preview if it exists
                const preview = conversationItem.querySelector('.conversation-last-message');
                if (preview) {
                    preview.textContent = message.content;
                }
            }
        }
    }
    
    // Mark message as read
    function markMessageAsRead(messageId) {
        if (connection && connection.state === signalR.HubConnectionState.Connected) {
            connection.invoke('MarkMessageAsRead', messageId)
                .catch(error => {
                    console.error(`Error marking message ${messageId} as read:`, error);
                });
        }
    }
    
    // Handle message read status update
    function handleMessageRead(data) {
        console.log('Message marked as read:', data);
        
        // Update UI to show message as read
        const messageElement = document.querySelector(`.message[data-message-id="${data.messageId}"]`);
        if (messageElement) {
            const statusElement = messageElement.querySelector('.message-status');
            if (statusElement) {
                statusElement.className = 'message-status read';
                statusElement.textContent = 'Read';
            }
        }
    }
    
    // Handle local user typing
    function handleLocalUserTyping() {
        if (!activeConversationId || !connection || connection.state !== signalR.HubConnectionState.Connected) {
            return;
        }
        
        // If not already marked as typing, send the notification
        if (!isTyping) {
            isTyping = true;
            connection.invoke('UserTyping', activeConversationId)
                .catch(error => {
                    console.error('Error sending typing indicator:', error);
                });
        }
        
        // Reset the timeout
        clearTimeout(typingTimeout);
        
        // After 3 seconds of no typing, reset the flag
        typingTimeout = setTimeout(() => {
            isTyping = false;
        }, 3000);
    }
    
    // Handle remote user typing
    function handleUserTyping(data) {
        console.log('User is typing:', data);
        
        // Show typing indicator in UI
        const typingIndicator = document.querySelector('.typing-indicator');
        if (typingIndicator) {
            typingIndicator.textContent = `${data.username} is typing...`;
            typingIndicator.classList.add('visible');
            
            // Hide after 3 seconds
            setTimeout(() => {
                typingIndicator.classList.remove('visible');
            }, 3000);
        }
    }
    
    // Handle new chat notification
    function handleNewChatNotification(data) {
        console.log('New chat notification:', data);
        
        // Show notification if not in this conversation
        if (activeConversationId !== data.conversationId) {
            // Update conversation in list if visible
            const conversationItem = document.querySelector(`.conversation-item[data-conversation-id="${data.conversationId}"]`);
            if (conversationItem) {
                conversationItem.classList.add('has-new-message');
                
                // Update preview if exists
                const preview = conversationItem.querySelector('.conversation-last-message');
                if (preview) {
                    preview.textContent = data.content;
                }
            }
            
            // Show toast notification
            showToast(`New message from ${data.senderName}`, data.content);
        }
    }
    
    // Show toast notification
    function showToast(title, message) {
        // Create toast container if it doesn't exist
        let toastContainer = document.querySelector('.toast-container');
        if (!toastContainer) {
            toastContainer = document.createElement('div');
            toastContainer.className = 'toast-container';
            toastContainer.style.cssText = 'position: fixed; bottom: 20px; right: 20px; z-index: 1050;';
            document.body.appendChild(toastContainer);
        }
        
        // Create toast
        const toastId = `toast-${Date.now()}`;
        const toast = document.createElement('div');
        toast.id = toastId;
        toast.className = 'toast';
        toast.innerHTML = `
            <div class="toast-header">
                <strong>${title}</strong>
                <button type="button" class="close" onclick="this.parentNode.parentNode.remove()">×</button>
            </div>
            <div class="toast-body">
                ${message}
            </div>
        `;
        
        // Add to container
        toastContainer.appendChild(toast);
        
        // Auto-remove after 5 seconds
        setTimeout(() => {
            const toastElement = document.getElementById(toastId);
            if (toastElement) {
                toastElement.remove();
            }
        }, 5000);
    }
    
    // Public API
    return {
        init: init,
        sendMessage: sendMessage,
        joinConversation: setActiveConversation
    };
})();

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    try {
        // Wait for everything to load
        setTimeout(() => {
            ChatManager.init();
        }, 1000);
    } catch (error) {
        console.error("Error initializing ChatManager:", error);
    }
}); 