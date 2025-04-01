using Microsoft.AspNetCore.Mvc;
using DoAnWeb.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace DoAnWeb.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly DevCommunityContext _context;
        
        public SearchController(DevCommunityContext context)
        {
            _context = context;
        }
        
        [HttpGet]
        public IActionResult Search(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            {
                return BadRequest("Search query must be at least 2 characters");
            }
            
            // Normalize query
            string normalizedQuery = q.ToLower().Trim();
            
            // Limit number of results per category
            const int maxResults = 5;
            
            // Get questions matching query (title or content)
            var questions = _context.Questions
                .Where(question => 
                    question.Title.ToLower().Contains(normalizedQuery) || 
                    question.Body.ToLower().Contains(normalizedQuery))
                .OrderByDescending(q => q.CreatedDate)
                .Take(maxResults)
                .Select(question => new
                {
                    id = question.QuestionId,
                    title = question.Title,
                    answerCount = question.Answers.Count,
                    viewCount = question.ViewCount
                })
                .ToList();
            
            // Get tags matching query
            var tags = _context.Tags
                .Where(tag => tag.TagName.ToLower().Contains(normalizedQuery))
                .OrderByDescending(t => t.Questions.Count)
                .Take(maxResults)
                .Select(tag => new
                {
                    id = tag.TagId,
                    name = tag.TagName,
                    count = tag.Questions.Count
                })
                .ToList();
            
            // Get users matching query (username or display name)
            var users = _context.Users
                .Where(user => 
                    user.Username.ToLower().Contains(normalizedQuery) || 
                    user.DisplayName.ToLower().Contains(normalizedQuery))
                .OrderByDescending(u => u.ReputationPoints)
                .Take(maxResults)
                .Select(user => new
                {
                    id = user.UserId,
                    username = user.Username,
                    displayName = user.DisplayName,
                    avatar = string.IsNullOrEmpty(user.ProfilePicture) ? user.AvatarUrl : user.ProfilePicture
                })
                .ToList();
            
            // Get repositories matching query (name or description)
            var repositories = _context.Repositories
                .Where(repo => 
                    repo.RepositoryName.ToLower().Contains(normalizedQuery) || 
                    (repo.Description != null && repo.Description.ToLower().Contains(normalizedQuery)))
                .OrderByDescending(r => r.CreatedDate)
                .Take(maxResults)
                .Select(repo => new
                {
                    id = repo.RepositoryId,
                    name = repo.RepositoryName,
                    description = repo.Description,
                    owner = repo.Owner != null ? repo.Owner.Username : "",
                    createdDate = repo.CreatedDate
                })
                .ToList();
            
            // Construct and return result object
            var result = new
            {
                query = q,
                questions,
                tags,
                users,
                repositories
            };
            
            return Ok(result);
        }
    }
} 