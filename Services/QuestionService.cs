using DoAnWeb.Models;
using DoAnWeb.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DoAnWeb.Services
{
    /// <summary>
    /// Implementation of the IQuestionService interface
    /// Provides business logic for question-related operations
    /// </summary>
    public class QuestionService : IQuestionService
    {
        private readonly IQuestionRepository _questionRepository;
        private readonly IRepository<Tag> _tagRepository;
        private readonly IRepository<Vote> _voteRepository;
        private readonly IRepository<Answer> _answerRepository;
        private readonly IQuestionRealTimeService _realTimeService;
        private readonly INotificationService _notificationService;
        private readonly DevCommunityContext _context;

        /// <summary>
        /// Constructor with dependency injection for required repositories
        /// </summary>
        public QuestionService(
            IQuestionRepository questionRepository,
            IRepository<Tag> tagRepository,
            IRepository<Vote> voteRepository,
            IRepository<Answer> answerRepository,
            IQuestionRealTimeService realTimeService,
            INotificationService notificationService,
            DevCommunityContext context)
        {
            _questionRepository = questionRepository;
            _tagRepository = tagRepository;
            _voteRepository = voteRepository;
            _answerRepository = answerRepository;
            _realTimeService = realTimeService;
            _notificationService = notificationService;
            _context = context;
        }

        /// <summary>
        /// Retrieves all questions from the database
        /// </summary>
        public IEnumerable<Question> GetAllQuestions()
        {
            return _questionRepository.GetAll();
        }

        /// <summary>
        /// Retrieves all questions with their associated user information
        /// </summary>
        public IEnumerable<Question> GetQuestionsWithUsers()
        {
            return _questionRepository.GetQuestionsWithUsers();
        }

        /// <summary>
        /// Retrieves a specific question by its ID
        /// </summary>
        public Question GetQuestionById(int id)
        {
            return _questionRepository.GetById(id);
        }

        /// <summary>
        /// Retrieves a question with all its details including answers, comments, and tags
        /// View count handling is now done separately via ViewCountHub
        /// </summary>
        public Question GetQuestionWithDetails(int questionId)
        {
            var question = _context.Questions
                .Include(q => q.User)
                .Include(q => q.QuestionTags)
                    .ThenInclude(qt => qt.Tag)
                .Include(q => q.Attachments)
                .Include(q => q.Answers)
                    .ThenInclude(a => a.User)
                .Include(q => q.Answers)
                    .ThenInclude(a => a.Attachments)
                .FirstOrDefault(q => q.QuestionId == questionId);

            if (question == null)
                return null;

            // Load comments separately to structure them properly with replies
            // Get all question-related comments (direct comments and replies)
            var allQuestionComments = _context.Comments
                .Include(c => c.User)
                .Where(c => c.QuestionId == questionId && c.AnswerId == null)
                .ToList();

            // Separate into parent comments and replies
            var parentComments = allQuestionComments
                .Where(c => c.ParentCommentId == null)
                .ToList();

            var replies = allQuestionComments
                .Where(c => c.ParentCommentId != null)
                .ToList();

            // Group replies by parent comment ID
            var repliesByParent = replies
                .GroupBy(r => r.ParentCommentId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Assign replies to their parent comments
            foreach (var parentComment in parentComments)
            {
                if (repliesByParent.TryGetValue(parentComment.CommentId, out var commentReplies))
                {
                    parentComment.Replies = commentReplies;
                }
            }

            // Assign the structured comments to the question
            question.Comments = parentComments;

            // Do the same for each answer's comments
            if (question.Answers != null)
            {
                foreach (var answer in question.Answers)
                {
                    // Get all answer-related comments (direct comments and replies)
                    var allAnswerComments = _context.Comments
                        .Include(c => c.User)
                        .Where(c => c.AnswerId == answer.AnswerId)
                        .ToList();

                    // Separate into parent comments and replies
                    var answerParentComments = allAnswerComments
                        .Where(c => c.ParentCommentId == null)
                        .ToList();

                    var answerReplies = allAnswerComments
                        .Where(c => c.ParentCommentId != null)
                        .ToList();

                    // Group replies by parent comment ID
                    var answerRepliesByParent = answerReplies
                        .GroupBy(r => r.ParentCommentId)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    // Assign replies to their parent comments
                    foreach (var parentComment in answerParentComments)
                    {
                        if (answerRepliesByParent.TryGetValue(parentComment.CommentId, out var commentReplies))
                        {
                            parentComment.Replies = commentReplies;
                        }
                    }

                    // Assign the structured comments to the answer
                    answer.Comments = answerParentComments;
                }
            }

            return question;
        }

        /// <summary>
        /// Retrieves all questions that have a specific tag
        /// </summary>
        public IEnumerable<Question> GetQuestionsByTag(string tagName)
        {
            return _questionRepository.GetQuestionsByTag(tagName);
        }

        /// <summary>
        /// Retrieves all questions created by a specific user
        /// </summary>
        public IEnumerable<Question> GetQuestionsByUser(int userId)
        {
            return _questionRepository.GetQuestionsByUser(userId);
        }

        /// <summary>
        /// Searches for questions matching the provided search term
        /// Searches in title, body, and tags
        /// </summary>
        public IEnumerable<Question> SearchQuestions(string searchTerm)
        {
            return _questionRepository.SearchQuestions(searchTerm);
        }

        /// <summary>
        /// Creates a new question with optional tags
        /// Sets default values and processes tag associations
        /// </summary>
        public void CreateQuestion(Question question, List<string> tagNames)
        {
            // Set creation date and default values
            question.CreatedDate = DateTime.Now;
            question.UpdatedDate = DateTime.Now;
            question.ViewCount = 0;
            question.Score = 0;
            question.Status = "Open";

            // Process tags if provided
            if (tagNames != null && tagNames.Count > 0)
            {
                question.Tags = new List<Tag>();
                foreach (var tagName in tagNames)
                {
                    // Check if tag exists
                    var tag = _tagRepository.Find(t => t.TagName == tagName).FirstOrDefault();
                    if (tag == null)
                    {
                        // Create new tag if it doesn't exist
                        tag = new Tag { TagName = tagName };
                        _tagRepository.Add(tag);
                        _tagRepository.Save();
                    }

                    // Associate tag with question
                    question.Tags.Add(tag);
                }
            }

            // Create question in database
            _questionRepository.Add(question);
            _questionRepository.Save();

            // Notify clients about the new question
            _realTimeService.NotifyNewQuestion(question);
        }

        /// <summary>
        /// Updates an existing question and its associated tags
        /// Handles tag additions and removals
        /// </summary>
        public void UpdateQuestion(Question question, List<string> tagNames)
        {
            // Get existing question with details
            var existingQuestion = _questionRepository.GetQuestionWithDetails(question.QuestionId);
            if (existingQuestion == null)
                throw new ArgumentException("Question not found");

            // Update basic properties
            existingQuestion.Title = question.Title;
            existingQuestion.Body = question.Body;
            existingQuestion.UpdatedDate = DateTime.Now;
            
            // Update attachments if provided
            if (question.Attachments != null && question.Attachments.Count > 0)
            {
                // Add new attachments to existing collection
                foreach (var attachment in question.Attachments)
                {
                    if (!existingQuestion.Attachments.Any(a => a.AttachmentId == attachment.AttachmentId))
                    {
                        existingQuestion.Attachments.Add(attachment);
                    }
                }
            }

            // Update tags if provided
            if (tagNames != null)
            {
                // Clear existing tags
                existingQuestion.Tags.Clear();
                
                // Add new tags
                foreach (var tagName in tagNames)
                {
                    // Check if tag exists
                    var tag = _tagRepository.Find(t => t.TagName == tagName).FirstOrDefault();
                    if (tag == null)
                    {
                        // Create new tag
                        tag = new Tag { TagName = tagName };
                        _tagRepository.Add(tag);
                        _tagRepository.Save();
                    }

                    // Associate tag with question
                    existingQuestion.Tags.Add(tag);
                }
            }

            // Save changes
            _questionRepository.Update(existingQuestion);
            _questionRepository.Save();

            // Notify clients about the question update
            _realTimeService.NotifyQuestionUpdated(existingQuestion);
        }

        /// <summary>
        /// Deletes a question and all its associated data
        /// </summary>
        public void DeleteQuestion(int id)
        {
            var question = _questionRepository.GetById(id);
            if (question != null)
            {
                _questionRepository.Delete(question);
                _questionRepository.Save();
            }
        }

        /// <summary>
        /// Records a vote (upvote or downvote) for a question
        /// Updates the question's score accordingly
        /// </summary>
        public void VoteQuestion(int questionId, int userId, bool isUpvote)
        {
            // Get existing vote if any
            var existingVote = _voteRepository.Find(v => 
                v.TargetId == questionId && 
                v.UserId == userId && 
                v.TargetType == "Question").FirstOrDefault();

            // Determine vote value
            int voteValue = isUpvote ? 1 : -1;

            if (existingVote != null)
            {
                // If vote exists with same value, remove it (toggle off)
                if (existingVote.VoteValue == voteValue)
                {
                    _voteRepository.Delete(existingVote);
                    _voteRepository.Save();
                    
                    // Update question score
                    UpdateQuestionScore(questionId, -voteValue);
                }
                // If vote exists with different value, update it
                else
                {
                    existingVote.VoteValue = voteValue;
                    _voteRepository.Update(existingVote);
                    _voteRepository.Save();
                    
                    // Update question score (double effect: remove old vote, add new vote)
                    UpdateQuestionScore(questionId, voteValue * 2);
                }
            }
            else
            {
                // Create new vote
                var vote = new Vote
                {
                    TargetId = questionId,
                    UserId = userId,
                    TargetType = "Question",
                    VoteValue = voteValue,
                    IsUpvote = isUpvote,
                    CreatedDate = DateTime.Now
                };
                
                _voteRepository.Add(vote);
                _voteRepository.Save();
                
                // Update question score
                UpdateQuestionScore(questionId, voteValue);
                
                // Send notification about the new vote
                _notificationService.NotifyVoteAsync("Question", questionId, userId);
            }
        }

        /// <summary>
        /// Helper method to update a question's score
        /// </summary>
        private void UpdateQuestionScore(int questionId, int scoreChange)
        {
            var question = _questionRepository.GetById(questionId);
            if (question != null)
            {
                question.Score += scoreChange;
                _questionRepository.Update(question);
                _questionRepository.Save();
            }
        }

        /// <summary>
        /// Adds a new answer to a question
        /// </summary>
        public void AddAnswer(Answer answer)
        {
            answer.CreatedDate = DateTime.Now;
            
            _answerRepository.Add(answer);
            _answerRepository.Save();
            
            // Update question's UpdatedDate to show activity
            if (answer.QuestionId.HasValue)
            {
                var question = _questionRepository.GetById(answer.QuestionId.Value);
                if (question != null)
                {
                    question.UpdatedDate = DateTime.Now;
                    _questionRepository.Update(question);
                    _questionRepository.Save();
                }
                
                // Notify clients about the new answer
                _realTimeService.NotifyNewAnswer(answer);
                
                // Send notification to question owner about the new answer
                if (answer.UserId.HasValue)
                {
                    _notificationService.NotifyNewAnswerAsync(answer.QuestionId.Value, answer.AnswerId, answer.UserId.Value);
                }
            }
        }
        
        public void AddAnswerAttachment(AnswerAttachment attachment)
        {
            var context = _questionRepository.GetContext() as DevCommunityContext;
            context.AnswerAttachments.Add(attachment);
            context.SaveChanges();
        }
        
        /// <summary>
        /// Adds a new attachment to a question
        /// </summary>
        public void AddAttachment(QuestionAttachment attachment)
        {
            var context = _questionRepository.GetContext() as DevCommunityContext;
            context.QuestionAttachments.Add(attachment);
            context.SaveChanges();
        }

        /// <summary>
        /// Gets a user's vote on a specific question
        /// </summary>
        public Vote GetUserVoteOnQuestion(int userId, int questionId)
        {
            return _voteRepository.Find(v => 
                v.UserId == userId && 
                v.TargetId == questionId && 
                v.TargetType == "Question").FirstOrDefault();
        }

        /// <summary>
        /// Gets a user's vote on a specific answer
        /// </summary>
        public Vote GetUserVoteOnAnswer(int userId, int answerId)
        {
            return _voteRepository.Find(v => 
                v.UserId == userId && 
                v.TargetId == answerId && 
                v.TargetType == "Answer").FirstOrDefault();
        }

        /// <summary>
        /// Adds a new vote
        /// </summary>
        public void AddVote(Vote vote)
        {
            _voteRepository.Add(vote);
            _voteRepository.Save();
            
            // Update the target's score
            if (vote.TargetType == "Question")
            {
                UpdateQuestionScore(vote.TargetId, vote.IsUpvote ? 1 : -1);
            }
            else if (vote.TargetType == "Answer")
            {
                UpdateAnswerScore(vote.TargetId, vote.IsUpvote ? 1 : -1);
            }
            
            // Send notification about the new vote if UserId has a value
            if (vote.UserId.HasValue)
            {
                _notificationService.NotifyVoteAsync(vote.TargetType, vote.TargetId, vote.UserId.Value);
            }
        }

        /// <summary>
        /// Updates an existing vote
        /// </summary>
        public void UpdateVote(Vote vote)
        {
            _voteRepository.Update(vote);
            _voteRepository.Save();
            
            // Update the target's score (double effect: remove old vote, add new vote)
            if (vote.TargetType == "Question")
            {
                UpdateQuestionScore(vote.TargetId, vote.IsUpvote ? 2 : -2);
            }
            else if (vote.TargetType == "Answer")
            {
                UpdateAnswerScore(vote.TargetId, vote.IsUpvote ? 2 : -2);
            }
        }

        /// <summary>
        /// Removes a vote by its ID
        /// </summary>
        public void RemoveVote(int voteId)
        {
            var vote = _voteRepository.GetById(voteId);
            if (vote != null)
            {
                // Lưu thông tin trước khi xóa
                int targetId = vote.TargetId;
                string targetType = vote.TargetType;
                int scoreChange = vote.IsUpvote ? -1 : 1; // Ngược lại với giá trị ban đầu khi xóa
                
                _voteRepository.Delete(vote);
                _voteRepository.Save();

                // Update the score for the affected entity
                if (targetType == "Question")
                {
                    UpdateQuestionScore(targetId, scoreChange);
                }
                else if (targetType == "Answer")
                {
                    UpdateAnswerScore(targetId, scoreChange);
                }
            }
        }
        
        /// <summary>
        /// Helper method to update an answer's score
        /// </summary>
        private void UpdateAnswerScore(int answerId, int scoreChange)
        {
            var context = _questionRepository.GetContext() as DevCommunityContext;
            var answer = context.Answers.FirstOrDefault(a => a.AnswerId == answerId);
            
            if (answer != null)
            {
                answer.Score += scoreChange;
                context.SaveChanges();
            }
        }

        /// <summary>
        /// Cập nhật lượt xem cho câu hỏi
        /// </summary>
        /// <param name="questionId">ID của câu hỏi</param>
        /// <returns>Số lượt xem mới của câu hỏi</returns>
        public int UpdateViewCount(int questionId)
        {
            var question = _questionRepository.GetById(questionId);
            if (question == null)
                throw new ArgumentException("Question not found");
            
            // Tăng lượt xem
            question.ViewCount = (question.ViewCount ?? 0) + 1;
            _questionRepository.Update(question);
            _questionRepository.Save();
            
            return question.ViewCount ?? 0;
        }
        
        /// <summary>
        /// Lấy số lượt xem hiện tại cho câu hỏi
        /// </summary>
        /// <param name="questionId">ID của câu hỏi</param>
        /// <returns>Số lượt xem hiện tại</returns>
        public int GetViewCount(int questionId)
        {
            var question = _questionRepository.GetById(questionId);
            if (question == null)
                throw new ArgumentException("Question not found");
            
            return question.ViewCount ?? 0;
        }

        /// <summary>
        /// Retrieves all questions from the database asynchronously
        /// </summary>
        public async Task<IEnumerable<Question>> GetAllQuestionsAsync()
        {
            return await _questionRepository.GetAllAsync();
        }

        /// <summary>
        /// Retrieves all questions with their associated user information asynchronously
        /// </summary>
        public async Task<IEnumerable<Question>> GetQuestionsWithUsersAsync()
        {
            // Since the repository might not have an async version, we'll use the context directly
            return await _context.Questions
                .Include(q => q.User)
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves a specific question by its ID asynchronously
        /// </summary>
        public async Task<Question> GetQuestionByIdAsync(int id)
        {
            return await _questionRepository.GetByIdAsync(id);
        }

        /// <summary>
        /// Retrieves a question with all its details including answers, comments, and tags asynchronously
        /// </summary>
        public async Task<Question> GetQuestionWithDetailsAsync(int questionId)
        {
            var question = await _context.Questions
                .Include(q => q.User)
                .Include(q => q.QuestionTags)
                    .ThenInclude(qt => qt.Tag)
                .Include(q => q.Attachments)
                .Include(q => q.Answers)
                    .ThenInclude(a => a.User)
                .Include(q => q.Answers)
                    .ThenInclude(a => a.Attachments)
                .FirstOrDefaultAsync(q => q.QuestionId == questionId);

            if (question == null)
                return null;

            // Load comments separately to structure them properly with replies
            // Get all question-related comments (direct comments and replies)
            var allQuestionComments = await _context.Comments
                .Include(c => c.User)
                .Where(c => c.QuestionId == questionId && c.AnswerId == null)
                .ToListAsync();

            // Separate into parent comments and replies
            var parentComments = allQuestionComments
                .Where(c => c.ParentCommentId == null)
                .ToList();
            
            // Group replies by parent comment ID
            var repliesByParent = allQuestionComments
                .Where(c => c.ParentCommentId != null)
                .GroupBy(c => c.ParentCommentId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Assign replies to their parent comments
            foreach (var parentComment in parentComments)
            {
                if (repliesByParent.TryGetValue(parentComment.CommentId, out var commentReplies))
                {
                    parentComment.Replies = commentReplies;
                }
            }

            // Assign the structured comments to the question
            question.Comments = parentComments;

            // Do the same for each answer's comments
            if (question.Answers != null)
            {
                foreach (var answer in question.Answers)
                {
                    // Get all answer-related comments (direct comments and replies)
                    var allAnswerComments = await _context.Comments
                        .Include(c => c.User)
                        .Where(c => c.AnswerId == answer.AnswerId)
                        .ToListAsync();

                    // Separate into parent comments and replies
                    var answerParentComments = allAnswerComments
                        .Where(c => c.ParentCommentId == null)
                        .ToList();
                    
                    // Group replies by parent comment ID
                    var answerRepliesByParent = allAnswerComments
                        .Where(c => c.ParentCommentId != null)
                        .GroupBy(c => c.ParentCommentId)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    // Assign replies to their parent comments
                    foreach (var parentComment in answerParentComments)
                    {
                        if (answerRepliesByParent.TryGetValue(parentComment.CommentId, out var commentReplies))
                        {
                            parentComment.Replies = commentReplies;
                        }
                    }

                    // Assign the structured comments to the answer
                    answer.Comments = answerParentComments;
                }
            }

            return question;
        }

        /// <summary>
        /// Retrieves all questions that have a specific tag asynchronously
        /// </summary>
        public async Task<IEnumerable<Question>> GetQuestionsByTagAsync(string tagName)
        {
            // Using context directly for async operation
            return await _context.Questions
                .Include(q => q.User)
                .Include(q => q.QuestionTags)
                    .ThenInclude(qt => qt.Tag)
                .Where(q => q.QuestionTags.Any(qt => qt.Tag.TagName == tagName))
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves all questions created by a specific user asynchronously
        /// </summary>
        public async Task<IEnumerable<Question>> GetQuestionsByUserAsync(int userId)
        {
            return await _context.Questions
                .Include(q => q.User)
                .Where(q => q.UserId == userId)
                .ToListAsync();
        }

        /// <summary>
        /// Searches for questions matching the provided search term asynchronously
        /// Searches in title, body, and tags
        /// </summary>
        public async Task<IEnumerable<Question>> SearchQuestionsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllQuestionsAsync();

            // Using context directly for async operation with search
            var normalizedSearchTerm = searchTerm.ToLower();
            
            return await _context.Questions
                .Include(q => q.User)
                .Include(q => q.QuestionTags)
                    .ThenInclude(qt => qt.Tag)
                .Include(q => q.Answers)
                .Where(q => 
                    q.Title.ToLower().Contains(normalizedSearchTerm) ||
                    q.Body.ToLower().Contains(normalizedSearchTerm) ||
                    q.QuestionTags.Any(qt => qt.Tag.TagName.ToLower().Contains(normalizedSearchTerm))
                )
                .ToListAsync();
        }

        /// <summary>
        /// Creates a new question with optional tags asynchronously
        /// </summary>
        public async Task CreateQuestionAsync(Question question, List<string> tagNames)
        {
            // Set default values if not provided
            question.CreatedDate = DateTime.Now;
            question.UpdatedDate = DateTime.Now;
            question.ViewCount = 0;
            question.Score = 0;
            
            // Add the question to the repository
            _questionRepository.Add(question);
            await _questionRepository.SaveAsync();
            
            // Associate tags with the question if provided
            if (tagNames != null && tagNames.Count > 0)
            {
                foreach (var tagName in tagNames)
                {
                    // Check if tag exists
                    var tag = (await _tagRepository.FindAsync(t => t.TagName == tagName)).FirstOrDefault();
                    if (tag == null)
                    {
                        // Create new tag
                        tag = new Tag { TagName = tagName };
                        _tagRepository.Add(tag);
                        await _tagRepository.SaveAsync();
                    }
                    
                    // Create question-tag association
                    var questionTag = new QuestionTag
                    {
                        QuestionId = question.QuestionId,
                        TagId = tag.TagId
                    };
                    
                    _context.QuestionTags.Add(questionTag);
                }
                
                // Save changes
                await _context.SaveChangesAsync();
            }
            
            // Notify clients about the new question
            await _realTimeService.NotifyNewQuestion(question);
        }

        /// <summary>
        /// Updates an existing question and its associated tags asynchronously
        /// </summary>
        public async Task UpdateQuestionAsync(Question question, List<string> tagNames)
        {
            var existingQuestion = await _questionRepository.GetByIdAsync(question.QuestionId);
            if (existingQuestion == null)
                throw new ArgumentException($"Question with ID {question.QuestionId} not found");
                
            // Update question properties
            existingQuestion.Title = question.Title;
            existingQuestion.Body = question.Body;
            existingQuestion.UpdatedDate = DateTime.Now;
            
            // Update tags if provided
            if (tagNames != null)
            {
                // Clear existing tags
                var existingQuestionTags = await _context.QuestionTags
                    .Where(qt => qt.QuestionId == question.QuestionId)
                    .ToListAsync();
                
                _context.QuestionTags.RemoveRange(existingQuestionTags);
                
                // Add new tags
                foreach (var tagName in tagNames)
                {
                    // Check if tag exists
                    var tag = (await _tagRepository.FindAsync(t => t.TagName == tagName)).FirstOrDefault();
                    if (tag == null)
                    {
                        // Create new tag
                        tag = new Tag { TagName = tagName };
                        _tagRepository.Add(tag);
                        await _tagRepository.SaveAsync();
                    }

                    // Associate tag with question
                    var questionTag = new QuestionTag
                    {
                        QuestionId = question.QuestionId,
                        TagId = tag.TagId
                    };
                    
                    _context.QuestionTags.Add(questionTag);
                }
            }

            // Save changes
            _questionRepository.Update(existingQuestion);
            await _questionRepository.SaveAsync();
            await _context.SaveChangesAsync();

            // Notify clients about the question update
            await _realTimeService.NotifyQuestionUpdated(existingQuestion);
        }

        /// <summary>
        /// Deletes a question and all its associated data asynchronously
        /// </summary>
        public async Task DeleteQuestionAsync(int id)
        {
            var question = await _questionRepository.GetByIdAsync(id);
            if (question != null)
            {
                _questionRepository.Delete(question);
                await _questionRepository.SaveAsync();
            }
        }

        /// <summary>
        /// Records a vote (upvote or downvote) for a question asynchronously
        /// Updates the question's score accordingly
        /// </summary>
        public async Task VoteQuestionAsync(int questionId, int userId, bool isUpvote)
        {
            // Get the question
            var question = await _questionRepository.GetByIdAsync(questionId);
            if (question == null)
                throw new ArgumentException($"Question with ID {questionId} not found");
            
            // Check if user has already voted on this question
            var existingVotes = await _voteRepository.FindAsync(v => 
                v.UserId == userId && 
                v.TargetId == questionId && 
                v.TargetType == "Question");
            
            var existingVote = existingVotes.FirstOrDefault();
            
            // Calculate vote value (1 for upvote, -1 for downvote)
            int voteValue = isUpvote ? 1 : -1;
            
            if (existingVote != null)
            {
                // User has already voted on this question
                if ((existingVote.IsUpvote && isUpvote) || (!existingVote.IsUpvote && !isUpvote))
                {
                    // Same vote type, remove the vote
                    _voteRepository.Delete(existingVote);
                    await _voteRepository.SaveAsync();
                    
                    // Adjust question score
                    await UpdateQuestionScoreAsync(questionId, -existingVote.VoteValue);
                }
                else
                {
                    // Changed vote direction
                    existingVote.IsUpvote = isUpvote;
                    existingVote.VoteValue = voteValue;
                    existingVote.CreatedDate = DateTime.Now;
                    
                    _voteRepository.Update(existingVote);
                    await _voteRepository.SaveAsync();
                    
                    // Adjust question score (double the value for a vote flip)
                    await UpdateQuestionScoreAsync(questionId, voteValue * 2);
                }
            }
            else
            {
                // Create new vote
                var vote = new Vote
                {
                    TargetId = questionId,
                    UserId = userId,
                    TargetType = "Question",
                    VoteValue = voteValue,
                    IsUpvote = isUpvote,
                    CreatedDate = DateTime.Now
                };
                
                _voteRepository.Add(vote);
                await _voteRepository.SaveAsync();
                
                // Update question score
                await UpdateQuestionScoreAsync(questionId, voteValue);
                
                // Send notification about the new vote
                await _notificationService.NotifyVoteAsync("Question", questionId, userId);
            }
        }

        /// <summary>
        /// Helper method to update a question's score asynchronously
        /// </summary>
        private async Task UpdateQuestionScoreAsync(int questionId, int scoreChange)
        {
            var question = await _questionRepository.GetByIdAsync(questionId);
            if (question != null)
            {
                question.Score += scoreChange;
                _questionRepository.Update(question);
                await _questionRepository.SaveAsync();
            }
        }

        /// <summary>
        /// Adds a new answer to a question asynchronously
        /// </summary>
        public async Task AddAnswerAsync(Answer answer)
        {
            // Set default values
            answer.CreatedDate = DateTime.Now;
            answer.UpdatedDate = DateTime.Now;
            answer.Score = 0;
            answer.IsAccepted = false;
            
            // Add the answer
            _answerRepository.Add(answer);
            await _answerRepository.SaveAsync();
            
            // Update question's answer count (if using cached counts)
            var question = await _questionRepository.GetByIdAsync(answer.QuestionId.Value);
            if (question != null)
            {
                question.UpdatedDate = DateTime.Now;
                _questionRepository.Update(question);
                await _questionRepository.SaveAsync();
            }
            
            // Send notification
            await _realTimeService.NotifyNewAnswer(answer);
            
            // Notify the question owner
            if (question?.UserId != null && answer.UserId != null && question.UserId != answer.UserId)
            {
                await _notificationService.NotifyNewAnswerAsync(question.QuestionId, answer.AnswerId, answer.UserId.Value);
            }
        }

        /// <summary>
        /// Adds a new attachment to a question asynchronously
        /// </summary>
        public async Task AddAttachmentAsync(QuestionAttachment attachment)
        {
            _context.QuestionAttachments.Add(attachment);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Adds a new attachment to an answer asynchronously
        /// </summary>
        public async Task AddAnswerAttachmentAsync(AnswerAttachment attachment)
        {
            _context.AnswerAttachments.Add(attachment);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Gets a user's vote on a specific question asynchronously
        /// </summary>
        public async Task<Vote> GetUserVoteOnQuestionAsync(int userId, int questionId)
        {
            var votes = await _voteRepository.FindAsync(v => 
                v.UserId == userId && 
                v.TargetId == questionId && 
                v.TargetType == "Question");
                
            return votes.FirstOrDefault();
        }

        /// <summary>
        /// Gets a user's vote on a specific answer asynchronously
        /// </summary>
        public async Task<Vote> GetUserVoteOnAnswerAsync(int userId, int answerId)
        {
            var votes = await _voteRepository.FindAsync(v => 
                v.UserId == userId && 
                v.TargetId == answerId && 
                v.TargetType == "Answer");
                
            return votes.FirstOrDefault();
        }

        /// <summary>
        /// Adds a new vote asynchronously
        /// </summary>
        public async Task AddVoteAsync(Vote vote)
        {
            _voteRepository.Add(vote);
            await _voteRepository.SaveAsync();
            
            // Update the target's score
            if (vote.TargetType == "Question")
            {
                await UpdateQuestionScoreAsync(vote.TargetId, vote.IsUpvote ? 1 : -1);
            }
            else if (vote.TargetType == "Answer")
            {
                await UpdateAnswerScoreAsync(vote.TargetId, vote.IsUpvote ? 1 : -1);
            }
            
            // Send notification about the new vote if UserId has a value
            if (vote.UserId.HasValue)
            {
                await _notificationService.NotifyVoteAsync(vote.TargetType, vote.TargetId, vote.UserId.Value);
            }
        }

        /// <summary>
        /// Updates an existing vote asynchronously
        /// </summary>
        public async Task UpdateVoteAsync(Vote vote)
        {
            var existingVote = (await _voteRepository.FindAsync(v => v.VoteId == vote.VoteId)).FirstOrDefault();
            if (existingVote == null)
                throw new ArgumentException($"Vote with ID {vote.VoteId} not found");
                
            // Calculate the score change based on the vote change
            int scoreChange = 0;
            
            if (existingVote.IsUpvote != vote.IsUpvote)
            {
                // If the vote direction changed, update score accordingly
                scoreChange = vote.IsUpvote ? 2 : -2; // +2 for switching from down to up, -2 for switching from up to down
            }
            
            // Update the vote
            existingVote.IsUpvote = vote.IsUpvote;
            existingVote.VoteValue = vote.IsUpvote ? 1 : -1;
            existingVote.VoteDate = vote.VoteDate;
            
            _voteRepository.Update(existingVote);
            await _voteRepository.SaveAsync();
            
            // Update the target's score if the vote direction changed
            if (scoreChange != 0)
            {
                if (existingVote.TargetType == "Question")
                {
                    await UpdateQuestionScoreAsync(existingVote.TargetId, scoreChange);
                }
                else if (existingVote.TargetType == "Answer")
                {
                    await UpdateAnswerScoreAsync(existingVote.TargetId, scoreChange);
                }
            }
        }

        /// <summary>
        /// Removes a vote by its ID asynchronously
        /// </summary>
        public async Task RemoveVoteAsync(int voteId)
        {
            var vote = (await _voteRepository.FindAsync(v => v.VoteId == voteId)).FirstOrDefault();
            if (vote == null)
                return;
                
            int scoreChange = -vote.VoteValue; // Negate the vote value to remove its effect
            
            _voteRepository.Delete(vote);
            await _voteRepository.SaveAsync();
            
            // Update the target's score
            if (vote.TargetType == "Question")
            {
                await UpdateQuestionScoreAsync(vote.TargetId, scoreChange);
            }
            else if (vote.TargetType == "Answer")
            {
                await UpdateAnswerScoreAsync(vote.TargetId, scoreChange);
            }
        }

        /// <summary>
        /// Updates answer score asynchronously
        /// </summary>
        private async Task UpdateAnswerScoreAsync(int answerId, int scoreChange)
        {
            var answer = await _answerRepository.GetByIdAsync(answerId);
            if (answer != null)
            {
                answer.Score += scoreChange;
                _answerRepository.Update(answer);
                await _answerRepository.SaveAsync();
            }
        }

        /// <summary>
        /// Cập nhật lượt xem cho câu hỏi dạng bất đồng bộ
        /// </summary>
        public async Task<int> UpdateViewCountAsync(int questionId)
        {
            var question = await _questionRepository.GetByIdAsync(questionId);
            if (question == null)
                return 0;
                
            // Increment view count
            question.ViewCount = (question.ViewCount ?? 0) + 1;
            
            _questionRepository.Update(question);
            await _questionRepository.SaveAsync();
            
            return question.ViewCount ?? 0;
        }

        /// <summary>
        /// Lấy số lượt xem hiện tại cho câu hỏi dạng bất đồng bộ
        /// </summary>
        public async Task<int> GetViewCountAsync(int questionId)
        {
            var question = await _questionRepository.GetByIdAsync(questionId);
            return question?.ViewCount ?? 0;
        }
    }
}