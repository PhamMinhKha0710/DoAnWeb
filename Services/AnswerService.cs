using DoAnWeb.Models;
using DoAnWeb.Repositories;

namespace DoAnWeb.Services
{
    public class AnswerService : IAnswerService
    {
        private readonly IRepository<Answer> _answerRepository;
        private readonly IRepository<Vote> _voteRepository;
        private readonly IQuestionRepository _questionRepository;

        public AnswerService(
            IRepository<Answer> answerRepository,
            IRepository<Vote> voteRepository,
            IQuestionRepository questionRepository)
        {
            _answerRepository = answerRepository;
            _voteRepository = voteRepository;
            _questionRepository = questionRepository;
        }

        public Answer GetAnswerById(int id)
        {
            return _answerRepository.GetById(id);
        }

        public Answer GetAnswerWithDetails(int id)
        {
            // Since we don't have a specific repository method for this,
            // we'll use the generic repository and include related entities
            var answer = _answerRepository.GetById(id);
            return answer;
        }

        public IEnumerable<Answer> GetAnswersByQuestion(int questionId)
        {
            return _answerRepository.Find(a => a.QuestionId == questionId);
        }

        public IEnumerable<Answer> GetAnswersByUser(int userId)
        {
            return _answerRepository.Find(a => a.UserId == userId);
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
        }

        public void UpdateAnswer(Answer answer)
        {
            if (answer == null)
                throw new ArgumentNullException(nameof(answer));

            var existingAnswer = _answerRepository.GetById(answer.AnswerId);
            if (existingAnswer == null)
                throw new ArgumentException("Answer not found");

            existingAnswer.Body = answer.Body;
            existingAnswer.UpdatedDate = DateTime.Now;

            _answerRepository.Update(existingAnswer);
            _answerRepository.Save();
        }

        public void DeleteAnswer(int id)
        {
            var answer = _answerRepository.GetById(id);
            if (answer == null)
                return;

            _answerRepository.Delete(id);
            _answerRepository.Save();
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
        }

        public void AcceptAnswer(int answerId, int questionId, int userId)
        {
            // Get answer
            var answer = _answerRepository.GetById(answerId);
            if (answer == null)
                throw new ArgumentException("Answer not found");

            // Get question
            var question = _questionRepository.GetById(questionId);
            if (question == null)
                throw new ArgumentException("Question not found");

            // Check if user is the author of the question
            if (question.UserId != userId)
                throw new UnauthorizedAccessException("Only the question author can accept an answer");

            // Check if answer belongs to the question
            if (answer.QuestionId != questionId)
                throw new ArgumentException("Answer does not belong to the specified question");

            // Reset any previously accepted answers for this question
            var acceptedAnswers = _answerRepository.Find(a => a.QuestionId == questionId && a.IsAccepted);
            foreach (var acceptedAnswer in acceptedAnswers)
            {
                acceptedAnswer.IsAccepted = false;
                _answerRepository.Update(acceptedAnswer);
            }

            // Accept the new answer
            answer.IsAccepted = true;
            _answerRepository.Update(answer);
            _answerRepository.Save();
        }
    }
}