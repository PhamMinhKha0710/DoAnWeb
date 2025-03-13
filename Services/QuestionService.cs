using DoAnWeb.Models;
using DoAnWeb.Repositories;

namespace DoAnWeb.Services
{
    public class QuestionService : IQuestionService
    {
        private readonly IQuestionRepository _questionRepository;
        private readonly IRepository<Tag> _tagRepository;
        private readonly IRepository<Vote> _voteRepository;
        private readonly IRepository<Answer> _answerRepository;

        public QuestionService(
            IQuestionRepository questionRepository,
            IRepository<Tag> tagRepository,
            IRepository<Vote> voteRepository,
            IRepository<Answer> answerRepository)
        {
            _questionRepository = questionRepository;
            _tagRepository = tagRepository;
            _voteRepository = voteRepository;
            _answerRepository = answerRepository;
        }

        public IEnumerable<Question> GetAllQuestions()
        {
            return _questionRepository.GetAll();
        }

        public IEnumerable<Question> GetQuestionsWithUsers()
        {
            return _questionRepository.GetQuestionsWithUsers();
        }

        public Question GetQuestionById(int id)
        {
            return _questionRepository.GetById(id);
        }

        public Question GetQuestionWithDetails(int id)
        {
            return _questionRepository.GetQuestionWithDetails(id);
        }

        public IEnumerable<Question> GetQuestionsByTag(string tagName)
        {
            return _questionRepository.GetQuestionsByTag(tagName);
        }

        public IEnumerable<Question> GetQuestionsByUser(int userId)
        {
            return _questionRepository.GetQuestionsByUser(userId);
        }

        public IEnumerable<Question> SearchQuestions(string searchTerm)
        {
            return _questionRepository.SearchQuestions(searchTerm);
        }

        public void CreateQuestion(Question question, List<string> tagNames)
        {
            // Set creation date
            question.CreatedDate = DateTime.Now;
            question.UpdatedDate = DateTime.Now;
            question.ViewCount = 0;
            question.Score = 0;
            question.Status = "Open";

            // Process tags
            if (tagNames != null && tagNames.Count > 0)
            {
                question.Tags = new List<Tag>();
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

                    question.Tags.Add(tag);
                }
            }

            // Create question
            _questionRepository.Add(question);
            _questionRepository.Save();
        }

        public void UpdateQuestion(Question question, List<string> tagNames)
        {
            // Get existing question
            var existingQuestion = _questionRepository.GetQuestionWithDetails(question.QuestionId);
            if (existingQuestion == null)
                throw new ArgumentException("Question not found");

            // Update question properties
            existingQuestion.Title = question.Title;
            existingQuestion.Body = question.Body;
            existingQuestion.UpdatedDate = DateTime.Now;

            // Update tags
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

                    existingQuestion.Tags.Add(tag);
                }
            }

            // Update question
            _questionRepository.Update(existingQuestion);
            _questionRepository.Save();
        }

        public void DeleteQuestion(int id)
        {
            // Get the question with its answers
            var question = _questionRepository.GetQuestionWithDetails(id);
            if (question == null)
                return;

            // Delete all answers associated with the question first
            if (question.Answers != null && question.Answers.Any())
            {
                foreach (var answer in question.Answers.ToList())
                {
                    _answerRepository.Delete(answer);
                }
            }

            // Now delete the question
            _questionRepository.Delete(id);
            _questionRepository.Save();
        }

        public void VoteQuestion(int questionId, int userId, bool isUpvote)
        {
            // Get question
            var question = _questionRepository.GetById(questionId);
            if (question == null)
                throw new ArgumentException("Question not found");

            // Check if user has already voted
            var existingVote = _voteRepository.Find(v => v.UserId == userId && v.TargetId == questionId && v.TargetType == "Question").FirstOrDefault();

            if (existingVote != null)
            {
                // Update existing vote
                int currentVoteValue = existingVote.VoteValue;
                int newVoteValue = isUpvote ? 1 : -1;
                
                if (currentVoteValue != newVoteValue)
                {
                    // Change vote direction
                    existingVote.VoteValue = newVoteValue;
                    _voteRepository.Update(existingVote);

                    // Update question score
                    question.Score += newVoteValue - currentVoteValue;
                }
                else
                {
                    // Remove vote
                    _voteRepository.Delete(existingVote);

                    // Update question score
                    question.Score -= currentVoteValue;
                }
            }
            else
            {
                // Create new vote
                var vote = new Vote
                {
                    UserId = userId,
                    TargetId = questionId,
                    TargetType = "Question",
                    VoteValue = isUpvote ? 1 : -1,
                    IsUpvote = isUpvote,
                    CreatedDate = DateTime.Now
                };

                _voteRepository.Add(vote);

                // Update question score
                question.Score += vote.VoteValue;
            }

            // Save changes
            _questionRepository.Update(question);
            _voteRepository.Save();
            _questionRepository.Save();
        }
         public void AddAnswer(Answer answer)
        {
            if (answer == null)
                throw new ArgumentNullException(nameof(answer));

            answer.CreatedDate = DateTime.Now;

            _answerRepository.Add(answer);
            _answerRepository.Save();
        }
    }
}