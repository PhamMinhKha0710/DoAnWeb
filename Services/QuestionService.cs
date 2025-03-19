using DoAnWeb.Models;
using DoAnWeb.Repositories;

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

        /// <summary>
        /// Constructor with dependency injection for required repositories
        /// </summary>
        public QuestionService(
            IQuestionRepository questionRepository,
            IRepository<Tag> tagRepository,
            IRepository<Vote> voteRepository,
            IRepository<Answer> answerRepository,
            IQuestionRealTimeService realTimeService,
            INotificationService notificationService)
        {
            _questionRepository = questionRepository;
            _tagRepository = tagRepository;
            _voteRepository = voteRepository;
            _answerRepository = answerRepository;
            _realTimeService = realTimeService;
            _notificationService = notificationService;
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
        public Question GetQuestionWithDetails(int id)
        {
            return _questionRepository.GetQuestionWithDetails(id);
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
    }
}