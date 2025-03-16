using DoAnWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DoAnWeb.Hubs
{
    /// <summary>
    /// SignalR hub for real-time chat functionality
    /// </summary>
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly DevCommunityContext _context;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(DevCommunityContext context, ILogger<ChatHub> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// When a user connects, join their personal groups
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            try
            {
                var userId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int userIdInt))
                {
                    // Add user to a group with their user ID for private messaging
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userIdInt}");
                    
                    // Add user to groups for all their conversations
                    var conversations = await _context.ConversationParticipants
                        .Where(cp => cp.UserId == userIdInt)
                        .Select(cp => cp.ConversationId)
                        .ToListAsync();
                    
                    foreach (var conversationId in conversations)
                    {
                        await Groups.AddToGroupAsync(Context.ConnectionId, $"Conversation_{conversationId}");
                    }
                    
                    // Store the connection ID in the user record
                    var user = await _context.Users.FindAsync(userIdInt);
                    if (user != null)
                    {
                        user.ConnectionId = Context.ConnectionId;
                        await _context.SaveChangesAsync();
                    }
                }
                
                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ChatHub.OnConnectedAsync");
                await base.OnConnectedAsync();
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int userIdInt))
            {
                // Remove user from their user group
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userIdInt}");
                
                // Remove user from all conversation groups
                var conversations = await _context.ConversationParticipants
                    .Where(cp => cp.UserId == userIdInt)
                    .Select(cp => cp.ConversationId)
                    .ToListAsync();
                
                foreach (var conversationId in conversations)
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Conversation_{conversationId}");
                }
                
                // Clear the connection ID in the user record
                var user = await _context.Users.FindAsync(userIdInt);
                if (user != null)
                {
                    user.ConnectionId = null;
                    await _context.SaveChangesAsync();
                }
            }
            
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Join a specific conversation group
        /// </summary>
        public async Task JoinConversation(int conversationId)
        {
            try
            {
                var userId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
                    return;
                
                // Verify user is part of the conversation
                var isParticipant = await _context.ConversationParticipants
                    .AnyAsync(p => p.ConversationId == conversationId && p.UserId == userIdInt);
                
                if (!isParticipant)
                    return;
                
                // Join the conversation group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Conversation_{conversationId}");
                
                // Notify others in the conversation
                await Clients.OthersInGroup($"Conversation_{conversationId}")
                    .SendAsync("UserJoinedConversation", new { UserId = userIdInt, ConversationId = conversationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error joining conversation {conversationId}");
            }
        }

        /// <summary>
        /// Send a message in a conversation
        /// </summary>
        public async Task SendMessage(int conversationId, string content)
        {
            try
            {
                // Get current user
                var userId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var username = Context.User.FindFirst(ClaimTypes.Name)?.Value;
                
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int senderId))
                    return;
                
                // Get the conversation
                var conversation = await _context.Conversations
                    .FindAsync(conversationId);
                
                if (conversation == null || !await _context.ConversationParticipants.AnyAsync(p => p.ConversationId == conversationId && p.UserId == senderId))
                    return;
                
                // Create and save the message
                var message = new Message
                {
                    ConversationId = conversationId,
                    SenderId = senderId,
                    Content = content,
                    SentAt = DateTime.UtcNow,
                    IsRead = false
                };
                
                _context.Messages.Add(message);
                await _context.SaveChangesAsync();
                
                // Broadcast to the conversation group
                await Clients.Group($"Conversation_{conversationId}").SendAsync("ReceiveMessage", new
                {
                    MessageId = message.MessageId,
                    SenderId = senderId,
                    SenderName = username,
                    Content = content,
                    SentAt = message.SentAt,
                    IsRead = false
                });
                
                // Update conversation's last activity time
                conversation.LastActivityAt = DateTime.UtcNow;
                _context.Conversations.Update(conversation);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending message to conversation {conversationId}");
            }
        }

        /// <summary>
        /// Mark message as read
        /// </summary>
        public async Task MarkMessageAsRead(int messageId)
        {
            var userId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                throw new HubException("User not authenticated");
            }

            var message = await _context.Messages
                .Include(m => m.Conversation)
                .FirstOrDefaultAsync(m => m.MessageId == messageId);
                
            if (message == null)
            {
                throw new HubException("Message not found");
            }

            // Check if user is part of the conversation
            var isParticipant = await _context.ConversationParticipants
                .AnyAsync(cp => cp.ConversationId == message.ConversationId && cp.UserId == userIdInt);
            
            if (!isParticipant)
            {
                throw new HubException("You are not a participant in this conversation");
            }

            // Only mark as read if the user is not the sender
            if (message.SenderId != userIdInt && !message.IsRead)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                // Notify the sender that their message has been read
                await Clients.Group($"User_{message.SenderId}").SendAsync("MessageRead", 
                    new { 
                        messageId = message.MessageId,
                        readBy = userIdInt,
                        readAt = message.ReadAt
                    });
            }
        }

        /// <summary>
        /// User is typing in a conversation
        /// </summary>
        public async Task UserTyping(int conversationId)
        {
            try
            {
                var userId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var username = Context.User.FindFirst(ClaimTypes.Name)?.Value;
                
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(username))
                    return;
                
                // Notify other users in the conversation that this user is typing
                await Clients.OthersInGroup($"Conversation_{conversationId}")
                    .SendAsync("UserTyping", new { UserId = userId, Username = username, ConversationId = conversationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UserTyping");
            }
        }

        public async Task SendMessageToConversation(int conversationId, string message)
        {
            var userId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                throw new HubException("User not authenticated");
            }

            // Check if user is part of the conversation
            var isParticipant = await _context.ConversationParticipants
                .AnyAsync(cp => cp.ConversationId == conversationId && cp.UserId == userIdInt);
            
            if (!isParticipant)
            {
                throw new HubException("You are not a participant in this conversation");
            }

            // Get all participants in the conversation
            var participants = await _context.ConversationParticipants
                .Where(cp => cp.ConversationId == conversationId && cp.UserId != userIdInt)
                .Select(cp => cp.UserId)
                .ToListAsync();

            // Create and save the message
            var newMessage = new Message
            {
                ConversationId = conversationId,
                SenderId = userIdInt,
                Content = message,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };
            
            _context.Messages.Add(newMessage);
            
            // Update the conversation's last activity time
            var conversation = await _context.Conversations.FindAsync(conversationId);
            if (conversation != null)
            {
                conversation.LastActivityAt = DateTime.UtcNow;
                _context.Conversations.Update(conversation);
            }
            
            await _context.SaveChangesAsync();

            // Send the message to the conversation group
            await Clients.Group($"Conversation_{conversationId}").SendAsync("ReceiveMessage", 
                new { 
                    messageId = newMessage.MessageId,
                    conversationId = newMessage.ConversationId,
                    senderId = newMessage.SenderId,
                    content = newMessage.Content,
                    sentAt = newMessage.SentAt
                });
        }

        public async Task CreateConversation(string title, int[] participantIds)
        {
            var userId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                throw new HubException("User not authenticated");
            }

            // Create the conversation
            var conversation = new Conversation
            {
                Title = title,
                CreatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow
            };
            
            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();

            // Add the creator as a participant
            var creatorParticipant = new ConversationParticipant
            {
                ConversationId = conversation.ConversationId,
                UserId = userIdInt,
                IsArchived = false,
                IsMuted = false
            };
            
            _context.ConversationParticipants.Add(creatorParticipant);

            // Add other participants
            foreach (var participantId in participantIds)
            {
                if (participantId != userIdInt)
                {
                    var participant = new ConversationParticipant
                    {
                        ConversationId = conversation.ConversationId,
                        UserId = participantId,
                        IsArchived = false,
                        IsMuted = false
                    };
                    
                    _context.ConversationParticipants.Add(participant);
                    
                    // Add the participant to the conversation group if they're online
                    var userConnections = await _context.Users
                        .Where(u => u.UserId == participantId)
                        .Select(u => u.ConnectionId)
                        .ToListAsync();
                    
                    foreach (var connectionId in userConnections.Where(c => !string.IsNullOrEmpty(c)))
                    {
                        await Groups.AddToGroupAsync(connectionId, $"Conversation_{conversation.ConversationId}");
                    }
                    
                    // Notify the participant about the new conversation
                    await Clients.Group($"User_{participantId}").SendAsync("NewConversation", 
                        new { 
                            conversationId = conversation.ConversationId,
                            title = conversation.Title,
                            createdBy = userIdInt,
                            createdAt = conversation.CreatedAt
                        });
                }
            }
            
            await _context.SaveChangesAsync();
            
            // Add the creator to the conversation group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Conversation_{conversation.ConversationId}");
            
            // Return the conversation details to the creator
            await Clients.Caller.SendAsync("ConversationCreated", 
                new { 
                    conversationId = conversation.ConversationId,
                    title = conversation.Title,
                    participants = participantIds.Append(userIdInt).Distinct().ToArray()
                });
        }
    }
} 