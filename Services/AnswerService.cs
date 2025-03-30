using DoAnWeb.Models;
using DoAnWeb.Repositories;

namespace DoAnWeb.Services
{
    public class AnswerService : IAnswerService
    {
        private readonly IRepository<Answer> _answerRepository;
        private readonly IRepository<Vote> _voteRepository;
        private readonly IQuestionRepository _questionRepository;
        private readonly IQuestionRealTimeService _realTimeService;
        private readonly INotificationService _notificationService;

        public AnswerService(
            IRepository<Answer> answerRepository,
            IRepository<Vote> voteRepository,
            IQuestionRepository questionRepository,
            IQuestionRealTimeService realTimeService,
            INotificationService notificationService)
        {
            _answerRepository = answerRepository;
            _voteRepository = voteRepository;
            _questionRepository = questionRepository;
            _realTimeService = realTimeService;
            _notificationService = notificationService;
        }

        public Answer GetAnswerById(int id)
        {
            return _answerRepository.GetById(id);
        }
        
        public async Task<Answer> GetAnswerByIdAsync(int id)
        {
            return await _answerRepository.GetByIdAsync(id);
        }

        public Answer GetAnswerWithDetails(int id)
        {
            // Since we don't have a specific repository method for this,
            // we'll use the generic repository and include related entities
            var answer = _answerRepository.GetById(id);
            return answer;
        }
        
        public async Task<Answer> GetAnswerWithDetailsAsync(int id)
        {
            // Since we don't have a specific repository method for this,
            // we'll use the generic repository and include related entities
            var answer = await _answerRepository.GetByIdAsync(id);
            return answer;
        }

        public IEnumerable<Answer> GetAnswersByQuestion(int questionId)
        {
            return _answerRepository.Find(a => a.QuestionId == questionId);
        }
        
        public async Task<IEnumerable<Answer>> GetAnswersByQuestionAsync(int questionId)
        {
            return await _answerRepository.FindAsync(a => a.QuestionId == questionId);
        }

        public IEnumerable<Answer> GetAnswersByUser(int userId)
        {
            return _answerRepository.Find(a => a.UserId == userId);
        }
        
        public async Task<IEnumerable<Answer>> GetAnswersByUserAsync(int userId)
        {
            return await _answerRepository.FindAsync(a => a.UserId == userId);
        }

        public void CreateAnswer(Answer answer)
        {
            if (answer == null)
                throw new ArgumentNullException(nameof(answer));

            answer.CreatedDate = DateTime.Now;
            answer.UpdatedDate = DateTime.Now;
            answer.Score = 0;
            answer.IsAccepted = false;

            _answerRepository.Add(answer);
            _answerRepository.Save();

            // Notify clients about the new answer
            _realTimeService.NotifyNewAnswer(answer);
            
            // Send notification to question owner about the new answer
            if (answer.QuestionId.HasValue && answer.UserId.HasValue)
            {
                _notificationService.NotifyNewAnswerAsync(answer.QuestionId.Value, answer.AnswerId, answer.UserId.Value);
            }
        }
        
        public async Task CreateAnswerAsync(Answer answer)
        {
            if (answer == null)
                throw new ArgumentNullException(nameof(answer));

            answer.CreatedDate = DateTime.Now;
            answer.UpdatedDate = DateTime.Now;
            answer.Score = 0;
            answer.IsAccepted = false;

            _answerRepository.Add(answer);
            await _answerRepository.SaveAsync();

            // Notify clients about the new answer
            await _realTimeService.NotifyNewAnswer(answer);
            
            // Send notification to question owner about the new answer
            if (answer.QuestionId.HasValue && answer.UserId.HasValue)
            {
                await _notificationService.NotifyNewAnswerAsync(answer.QuestionId.Value, answer.AnswerId, answer.UserId.Value);
            }
        }

        public void UpdateAnswer(Answer answer)
        {
            if (answer == null)
                throw new ArgumentNullException(nameof(answer));

            var existingAnswer = _answerRepository.GetById(answer.AnswerId);
            if (existingAnswer == null)
                throw new ArgumentException($"Answer with ID {answer.AnswerId} not found");

            existingAnswer.Body = answer.Body;
            existingAnswer.UpdatedDate = DateTime.Now;

            _answerRepository.Update(existingAnswer);
            _answerRepository.Save();

            // Notify clients about the answer update
            _realTimeService.NotifyNewAnswer(existingAnswer);
        }
        
        public async Task UpdateAnswerAsync(Answer answer)
        {
            if (answer == null)
                throw new ArgumentNullException(nameof(answer));

            var existingAnswer = await _answerRepository.GetByIdAsync(answer.AnswerId);
            if (existingAnswer == null)
                throw new ArgumentException($"Answer with ID {answer.AnswerId} not found");

            existingAnswer.Body = answer.Body;
            existingAnswer.UpdatedDate = DateTime.Now;

            _answerRepository.Update(existingAnswer);
            await _answerRepository.SaveAsync();

            // Notify clients about the answer update
            await _realTimeService.NotifyNewAnswer(existingAnswer);
        }

        public void DeleteAnswer(int id)
        {
            var answer = _answerRepository.GetById(id);
            if (answer == null)
                return;

            _answerRepository.Delete(id);
            _answerRepository.Save();
        }
        
        public async Task DeleteAnswerAsync(int id)
        {
            var answer = await _answerRepository.GetByIdAsync(id);
            if (answer == null)
                return;

            await _answerRepository.DeleteAsync(id);
            await _answerRepository.SaveAsync();
        }

        // Implement other async methods as needed...

        public async Task VoteAnswerAsync(int answerId, int userId, bool isUpvote)
        {
            // Get answer
            var answer = await _answerRepository.GetByIdAsync(answerId);
            if (answer == null)
                throw new ArgumentException("Answer not found");

            // Check if user has already voted
            var existingVotes = await _voteRepository.FindAsync(v => v.UserId == userId && v.TargetId == answerId && v.TargetType == "Answer");
            var existingVote = existingVotes.FirstOrDefault();

            if (existingVote != null)
            {
                // Update existing vote
                int currentVoteValue = existingVote.VoteValue;
                int newVoteValue = isUpvote ? 1 : -1;
                
                if (currentVoteValue != newVoteValue)
                {
                    // Change vote direction
                    existingVote.VoteValue = newVoteValue;
                    existingVote.IsUpvote = isUpvote;
                    _voteRepository.Update(existingVote);

                    // Update answer score
                    answer.Score += newVoteValue - currentVoteValue;
                }
                else
                {
                    // Remove vote
                    _voteRepository.Delete(existingVote);

                    // Update answer score
                    answer.Score -= currentVoteValue;
                }
            }
            else
            {
                // Create new vote
                var vote = new Vote
                {
                    UserId = userId,
                    TargetId = answerId,
                    TargetType = "Answer",
                    VoteValue = isUpvote ? 1 : -1,
                    IsUpvote = isUpvote,
                    CreatedDate = DateTime.Now
                };

                _voteRepository.Add(vote);

                // Update answer score
                answer.Score += vote.VoteValue;
            }

            // Save changes
            _answerRepository.Update(answer);
            await _voteRepository.SaveAsync();
            await _answerRepository.SaveAsync();
            
            // Send notification about the vote if it's a new vote or changed direction
            if (existingVote == null || (existingVote != null && existingVote.VoteValue != (isUpvote ? 1 : -1)))
            {
                await _notificationService.NotifyVoteAsync("Answer", answerId, userId);
            }
        }
        
        public void VoteAnswer(int answerId, int userId, bool isUpvote)
        {
            // Get answer
            var answer = _answerRepository.GetById(answerId);
            if (answer == null)
                throw new ArgumentException("Answer not found");

            // Check if user has already voted
            var existingVote = _voteRepository.Find(v => v.UserId == userId && v.TargetId == answerId && v.TargetType == "Answer").FirstOrDefault();

            if (existingVote != null)
            {
                // Update existing vote
                int currentVoteValue = existingVote.VoteValue;
                int newVoteValue = isUpvote ? 1 : -1;
                
                if (currentVoteValue != newVoteValue)
                {
                    // Change vote direction
                    existingVote.VoteValue = newVoteValue;
                    existingVote.IsUpvote = isUpvote;
                    _voteRepository.Update(existingVote);

                    // Update answer score
                    answer.Score += newVoteValue - currentVoteValue;
                }
                else
                {
                    // Remove vote
                    _voteRepository.Delete(existingVote);

                    // Update answer score
                    answer.Score -= currentVoteValue;
                }
            }
            else
            {
                // Create new vote
                var vote = new Vote
                {
                    UserId = userId,
                    TargetId = answerId,
                    TargetType = "Answer",
                    VoteValue = isUpvote ? 1 : -1,
                    IsUpvote = isUpvote,
                    CreatedDate = DateTime.Now
                };

                _voteRepository.Add(vote);

                // Update answer score
                answer.Score += vote.VoteValue;
            }

            // Save changes
            _answerRepository.Update(answer);
            _voteRepository.Save();
            _answerRepository.Save();
            
            // Send notification about the vote if it's a new vote or changed direction
            if (existingVote == null || (existingVote != null && existingVote.VoteValue != (isUpvote ? 1 : -1)))
            {
                _notificationService.NotifyVoteAsync("Answer", answerId, userId);
            }
        }
        
        public async Task AcceptAnswerAsync(int answerId, int questionId, int userId)
        {
            var answer = await _answerRepository.GetByIdAsync(answerId);
            if (answer == null)
                throw new ArgumentException($"Answer with ID {answerId} not found");

            // Get question to update the accepted answer ID
            var question = await _questionRepository.GetByIdAsync(questionId);
            if (question == null)
                throw new ArgumentException($"Question with ID {questionId} not found");

            // Verify the user is the question owner
            if (question.UserId != userId)
                throw new UnauthorizedAccessException("Only the question owner can accept an answer");

            // Set this answer as accepted
            answer.IsAccepted = true;
            _answerRepository.Update(answer);

            // Update question with the accepted answer ID
            question.Status = "Resolved";
            _questionRepository.Update(question);

            await _answerRepository.SaveAsync();
            await _questionRepository.SaveAsync();

            // Notify about the question update (status change to resolved)
            await _realTimeService.NotifyQuestionUpdated(question);
            
            // Notify about the answer update (accepted status)
            await _realTimeService.NotifyNewAnswer(answer);
            
            // Send notification to answer owner that their answer was accepted
            await _notificationService.NotifyAnswerAcceptedAsync(questionId, answerId);
        }

        public void AcceptAnswer(int answerId, int questionId, int userId)
        {
            var answer = _answerRepository.GetById(answerId);
            if (answer == null)
                throw new ArgumentException($"Answer with ID {answerId} not found");

            // Get question to update the accepted answer ID
            var question = _questionRepository.GetById(questionId);
            if (question == null)
                throw new ArgumentException($"Question with ID {questionId} not found");

            // Verify the user is the question owner
            if (question.UserId != userId)
                throw new UnauthorizedAccessException("Only the question owner can accept an answer");

            // Set this answer as accepted
            answer.IsAccepted = true;
            _answerRepository.Update(answer);

            // Update question with the accepted answer ID
            question.Status = "Resolved";
            _questionRepository.Update(question);

            _answerRepository.Save();
            _questionRepository.Save();

            // Notify about the question update (status change to resolved)
            _realTimeService.NotifyQuestionUpdated(question);
            
            // Notify about the answer update (accepted status)
            _realTimeService.NotifyNewAnswer(answer);
            
            // Send notification to answer owner that their answer was accepted
            _notificationService.NotifyAnswerAcceptedAsync(questionId, answerId);
        }
    }
}