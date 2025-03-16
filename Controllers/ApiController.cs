using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DoAnWeb.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace DoAnWeb.Controllers
{
    [Route("api")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly DevCommunityContext _context;

        public ApiController(DevCommunityContext context)
        {
            _context = context;
        }

        [Route("api/user/current")]
        [HttpGet]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return new JsonResult(new { success = false, message = "Not authenticated" });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == int.Parse(userId));
            if (user == null)
            {
                return new JsonResult(new { success = false, message = "User not found" });
            }

            return new JsonResult(new
            {
                success = true,
                userId = user.UserId,
                username = user.Username,
                email = user.Email
            });
        }

        [Route("api/signalr-status")]
        [HttpGet]
        public IActionResult GetSignalRStatus()
        {
            return new JsonResult(new
            {
                status = "available",
                hubs = new[]
                {
                    new { name = "notificationHub", path = "/notificationHub" },
                    new { name = "chatHub", path = "/chatHub" },
                    new { name = "presenceHub", path = "/presenceHub" },
                    new { name = "activityHub", path = "/activityHub" },
                    new { name = "questionHub", path = "/questionHub" }
                }
            });
        }

        [Route("api/test-conversation")]
        [HttpGet]
        public IActionResult GetTestConversation()
        {
            try
            {
                // Get current user ID
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
                {
                    return new JsonResult(new { success = false, message = "Authentication required" });
                }

                // Create a test conversation
                var conversation = new Conversation
                {
                    Title = "Test Conversation " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    CreatedAt = DateTime.Now,
                    LastActivityAt = DateTime.Now
                };
                _context.Conversations.Add(conversation);
                _context.SaveChanges();

                // Add current user as a participant
                var participant1 = new ConversationParticipant
                {
                    ConversationId = conversation.ConversationId,
                    UserId = userIdInt,
                    IsArchived = false,
                    IsMuted = false
                };
                _context.ConversationParticipants.Add(participant1);

                // Find another user to add to conversation
                var otherUser = _context.Users.Where(u => u.UserId != userIdInt).FirstOrDefault();
                if (otherUser != null)
                {
                    var participant2 = new ConversationParticipant
                    {
                        ConversationId = conversation.ConversationId,
                        UserId = otherUser.UserId,
                        IsArchived = false,
                        IsMuted = false
                    };
                    _context.ConversationParticipants.Add(participant2);
                }

                // Add a test message
                var message = new Message
                {
                    ConversationId = conversation.ConversationId,
                    SenderId = userIdInt,
                    Content = "Hello! This is a test message.",
                    SentAt = DateTime.Now,
                    IsRead = false
                };
                _context.Messages.Add(message);
                _context.SaveChanges();

                return new JsonResult(new { 
                    success = true, 
                    conversation = new {
                        id = conversation.ConversationId,
                        title = conversation.Title,
                        createdAt = conversation.CreatedAt,
                        participants = _context.ConversationParticipants
                            .Where(cp => cp.ConversationId == conversation.ConversationId)
                            .Select(cp => cp.UserId)
                            .ToList()
                    },
                    message = new {
                        id = message.MessageId,
                        content = message.Content,
                        sentAt = message.SentAt
                    }
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        [Route("api/conversations")]
        [HttpGet]
        public IActionResult GetConversations()
        {
            try
            {
                // Get current user ID
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
                {
                    return new JsonResult(new { success = false, message = "Authentication required" });
                }

                // Get conversations the user is part of
                var conversations = _context.ConversationParticipants
                    .Where(cp => cp.UserId == userIdInt)
                    .Select(cp => cp.Conversation)
                    .Select(c => new {
                        id = c.ConversationId,
                        title = c.Title,
                        createdAt = c.CreatedAt,
                        lastActivityAt = c.LastActivityAt,
                        participants = _context.ConversationParticipants
                            .Where(p => p.ConversationId == c.ConversationId)
                            .Select(p => new {
                                userId = p.UserId,
                                username = p.User.Username
                            })
                            .ToList(),
                        lastMessage = _context.Messages
                            .Where(m => m.ConversationId == c.ConversationId)
                            .OrderByDescending(m => m.SentAt)
                            .Select(m => new {
                                id = m.MessageId,
                                content = m.Content,
                                senderId = m.SenderId,
                                senderName = m.Sender.Username,
                                sentAt = m.SentAt,
                                isRead = m.IsRead
                            })
                            .FirstOrDefault()
                    })
                    .ToList();

                return new JsonResult(new { success = true, conversations });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        [Route("api/conversations/{conversationId}/messages")]
        [HttpGet]
        public IActionResult GetConversationMessages(int conversationId)
        {
            try
            {
                // Get current user ID
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
                {
                    return new JsonResult(new { success = false, message = "Authentication required" });
                }

                // Check if user is part of the conversation
                var isParticipant = _context.ConversationParticipants
                    .Any(cp => cp.ConversationId == conversationId && cp.UserId == userIdInt);
                
                if (!isParticipant)
                {
                    return new JsonResult(new { success = false, message = "You are not a participant in this conversation" });
                }

                // Get messages in the conversation
                var messages = _context.Messages
                    .Where(m => m.ConversationId == conversationId)
                    .OrderBy(m => m.SentAt)
                    .Select(m => new {
                        id = m.MessageId,
                        content = m.Content,
                        senderId = m.SenderId,
                        senderName = m.Sender.Username,
                        sentAt = m.SentAt,
                        isRead = m.IsRead,
                        readAt = m.ReadAt
                    })
                    .ToList();

                return new JsonResult(new { success = true, messages });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        [Route("api/current-user-info")]
        [HttpGet]
        public IActionResult GetCurrentUserInfo()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
                {
                    return new JsonResult(new { success = false, message = "Not authenticated" });
                }

                var user = _context.Users.FirstOrDefault(u => u.UserId == userIdInt);
                if (user == null)
                {
                    return new JsonResult(new { success = false, message = "User not found" });
                }

                return new JsonResult(new { 
                    success = true, 
                    userId = user.UserId,
                    username = user.Username,
                    email = user.Email,
                    profilePicture = user.ProfilePicture
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }
    }
} 