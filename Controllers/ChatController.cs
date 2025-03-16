using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DoAnWeb.Models;
using System.Security.Claims;

namespace DoAnWeb.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly DevCommunityContext _context;

        public ChatController(DevCommunityContext context)
        {
            _context = context;
        }

        // GET: /Chat
        public IActionResult Index()
        {
            return View();
        }

        // GET: /Chat/Conversations
        [HttpGet]
        public async Task<IActionResult> Conversations()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return Unauthorized();
            }

            var conversations = await _context.ConversationParticipants
                .Where(cp => cp.UserId == userIdInt && !cp.IsArchived)
                .Include(cp => cp.Conversation)
                .Select(cp => new
                {
                    cp.Conversation.ConversationId,
                    cp.Conversation.Title,
                    cp.Conversation.CreatedAt,
                    cp.Conversation.LastActivityAt,
                    UnreadCount = cp.Conversation.Messages.Count(m => m.SenderId != userIdInt && !m.IsRead),
                    LastMessage = cp.Conversation.Messages
                        .OrderByDescending(m => m.SentAt)
                        .Select(m => new
                        {
                            m.MessageId,
                            m.SenderId,
                            SenderName = m.Sender.Username,
                            m.Content,
                            m.SentAt,
                            m.IsRead
                        })
                        .FirstOrDefault(),
                    Participants = cp.Conversation.Participants
                        .Where(p => p.UserId != userIdInt)
                        .Select(p => new
                        {
                            p.UserId,
                            p.User.Username,
                            p.User.ProfilePicture
                        })
                        .ToList()
                })
                .OrderByDescending(c => c.LastActivityAt)
                .ToListAsync();

            return Json(conversations);
        }

        // GET: /Chat/Conversation/5
        [HttpGet]
        public async Task<IActionResult> Conversation(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return Unauthorized();
            }

            // Check if user is part of the conversation
            var isParticipant = await _context.ConversationParticipants
                .AnyAsync(cp => cp.ConversationId == id && cp.UserId == userIdInt);
            
            if (!isParticipant)
            {
                return Forbid();
            }

            var conversation = await _context.Conversations
                .Where(c => c.ConversationId == id)
                .Select(c => new
                {
                    c.ConversationId,
                    c.Title,
                    c.CreatedAt,
                    c.LastActivityAt,
                    Messages = c.Messages
                        .OrderBy(m => m.SentAt)
                        .Select(m => new
                        {
                            m.MessageId,
                            m.SenderId,
                            SenderName = m.Sender.Username,
                            SenderPicture = m.Sender.ProfilePicture,
                            m.Content,
                            m.SentAt,
                            m.IsRead,
                            m.ReadAt
                        })
                        .ToList(),
                    Participants = c.Participants
                        .Select(p => new
                        {
                            p.UserId,
                            p.User.Username,
                            p.User.ProfilePicture
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (conversation == null)
            {
                return NotFound();
            }

            return Json(conversation);
        }

        // GET: /Chat/Messages/5
        [HttpGet]
        public async Task<IActionResult> Messages(int conversationId, DateTime? before = null, int count = 20)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return Unauthorized();
            }

            // Check if user is part of the conversation
            var isParticipant = await _context.ConversationParticipants
                .AnyAsync(cp => cp.ConversationId == conversationId && cp.UserId == userIdInt);
            
            if (!isParticipant)
            {
                return Forbid();
            }

            var query = _context.Messages
                .Where(m => m.ConversationId == conversationId);
            
            if (before.HasValue)
            {
                query = query.Where(m => m.SentAt < before.Value);
            }

            var messages = await query
                .OrderByDescending(m => m.SentAt)
                .Take(count)
                .Select(m => new
                {
                    m.MessageId,
                    m.SenderId,
                    SenderName = m.Sender.Username,
                    SenderPicture = m.Sender.ProfilePicture,
                    m.Content,
                    m.SentAt,
                    m.IsRead,
                    m.ReadAt
                })
                .ToListAsync();

            return Json(messages.OrderBy(m => m.SentAt));
        }

        // GET: /Chat/Users
        [HttpGet]
        public async Task<IActionResult> Users(string search = null)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return Unauthorized();
            }

            var query = _context.Users.AsQueryable();
            
            // Exclude current user
            query = query.Where(u => u.UserId != userIdInt);
            
            // Apply search filter if provided
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.Username.Contains(search) || u.Email.Contains(search));
            }

            var users = await query
                .Select(u => new
                {
                    u.UserId,
                    u.Username,
                    u.ProfilePicture,
                    u.Email
                })
                .Take(20)
                .ToListAsync();

            return Json(users);
        }
    }
} 