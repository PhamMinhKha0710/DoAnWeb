using System;
using System.Collections.Generic;
using System.Linq;

namespace DoAnWeb.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public string? AvatarUrl { get; set; }

    public string? ProfilePicture { get; set; }

    public string? Bio { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime? LastLoginDate { get; set; }
    
    public string? ConnectionId { get; set; }
    
    public int Reputation
    {
        get
        {
            int reputation = 0;
            
            // Use null check to avoid NullReferenceException
            if (Votes != null)
            {
                // +10 for each upvote on answers - use Count instead of Count()
                reputation += Votes
                    .Count(v => v.TargetType == "Answer" && v.IsUpvote) * 10;
                
                // +5 for each upvote on questions - use Count instead of Count()
                reputation += Votes
                    .Count(v => v.TargetType == "Question" && v.IsUpvote) * 5;
                
                // -2 for each downvote - use Count instead of Count()
                reputation += Votes
                    .Count(v => !v.IsUpvote) * -2;
            }
            
            // +15 for each posted answer - use null check
            if (Answers != null)
            {
                reputation += Answers.Count * 15;
            }
            
            return reputation;
        }
    }
    
    public List<KeyValuePair<string, int>> TopTags(int count = 5)
    {
        // Create a dictionary to store tag counts
        var tagCounts = new Dictionary<string, int>();
        
        // Get tags from user's questions - use null check to avoid NullReferenceException
        if (Questions != null)
        {
            foreach (var question in Questions)
            {
                if (question.Tags != null)
                {
                    foreach (var tag in question.Tags)
                    {
                        if (tag.TagName != null)
                        {
                            if (tagCounts.ContainsKey(tag.TagName))
                                tagCounts[tag.TagName]++;
                            else
                                tagCounts[tag.TagName] = 1;
                        }
                    }
                }
            }
        }
        
        // Get tags from user's answers - use null check to avoid NullReferenceException
        if (Answers != null)
        {
            foreach (var answer in Answers)
            {
                if (answer.Question?.Tags != null)
                {
                    foreach (var tag in answer.Question.Tags)
                    {
                        if (tag.TagName != null)
                        {
                            if (tagCounts.ContainsKey(tag.TagName))
                                tagCounts[tag.TagName]++;
                            else
                                tagCounts[tag.TagName] = 1;
                        }
                    }
                }
            }
        }
        
        // Convert dictionary to list of KeyValuePair and sort
        return tagCounts
            .OrderByDescending(kvp => kvp.Value)
            .Take(count)
            .ToList();
    }

    public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();

    public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

    public virtual ICollection<Repository> Repositories { get; set; } = new List<Repository>();

    public virtual ICollection<RepositoryCommit> RepositoryCommits { get; set; } = new List<RepositoryCommit>();

    public virtual ICollection<UserSavedItem> UserSavedItems { get; set; } = new List<UserSavedItem>();

    public virtual ICollection<Vote> Votes { get; set; } = new List<Vote>();

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
