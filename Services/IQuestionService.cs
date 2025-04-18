using DoAnWeb.Models;

namespace DoAnWeb.Services
{
    /// <summary>
    /// Service interface for managing questions and their related operations
    /// Provides methods for CRUD operations and specialized question queries
    /// </summary>
    public interface IQuestionService
    {
        /// <summary>
        /// Retrieves all questions from the database
        /// </summary>
        /// <returns>Collection of all questions</returns>
        IEnumerable<Question> GetAllQuestions();
        
        /// <summary>
        /// Retrieves all questions asynchronously from the database
        /// </summary>
        /// <returns>Task containing collection of all questions</returns>
        Task<IEnumerable<Question>> GetAllQuestionsAsync();
        
        /// <summary>
        /// Retrieves all questions with their associated user information
        /// </summary>
        /// <returns>Collection of questions with user data</returns>
        IEnumerable<Question> GetQuestionsWithUsers();
        
        /// <summary>
        /// Retrieves all questions with their associated user information asynchronously
        /// </summary>
        /// <returns>Task containing collection of questions with user data</returns>
        Task<IEnumerable<Question>> GetQuestionsWithUsersAsync();
        
        /// <summary>
        /// Retrieves a specific question by its ID
        /// </summary>
        /// <param name="id">The question ID</param>
        /// <returns>The question if found, otherwise null</returns>
        Question GetQuestionById(int id);
        
        /// <summary>
        /// Retrieves a specific question by its ID asynchronously
        /// </summary>
        /// <param name="id">The question ID</param>
        /// <returns>Task containing the question if found, otherwise null</returns>
        Task<Question> GetQuestionByIdAsync(int id);
        
        /// <summary>
        /// Retrieves a question with all its details including answers, comments, and tags
        /// Also increments the view count for the question
        /// </summary>
        /// <param name="id">The question ID</param>
        /// <returns>The question with all related data if found, otherwise null</returns>
        Question GetQuestionWithDetails(int id);
        
        /// <summary>
        /// Retrieves a question with all its details including answers, comments, and tags asynchronously
        /// Also increments the view count for the question
        /// </summary>
        /// <param name="id">The question ID</param>
        /// <returns>Task containing the question with all related data if found, otherwise null</returns>
        Task<Question> GetQuestionWithDetailsAsync(int id);
        
        /// <summary>
        /// Retrieves all questions that have a specific tag
        /// </summary>
        /// <param name="tagName">The tag name to filter by</param>
        /// <returns>Collection of questions with the specified tag</returns>
        IEnumerable<Question> GetQuestionsByTag(string tagName);
        
        /// <summary>
        /// Retrieves all questions that have a specific tag asynchronously
        /// </summary>
        /// <param name="tagName">The tag name to filter by</param>
        /// <returns>Task containing collection of questions with the specified tag</returns>
        Task<IEnumerable<Question>> GetQuestionsByTagAsync(string tagName);
        
        /// <summary>
        /// Retrieves all questions created by a specific user
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>Collection of questions created by the specified user</returns>
        IEnumerable<Question> GetQuestionsByUser(int userId);
        
        /// <summary>
        /// Retrieves all questions created by a specific user asynchronously
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>Task containing collection of questions created by the specified user</returns>
        Task<IEnumerable<Question>> GetQuestionsByUserAsync(int userId);
        
        /// <summary>
        /// Searches for questions matching the provided search term
        /// Searches in title, body, and tags
        /// </summary>
        /// <param name="searchTerm">The search term</param>
        /// <returns>Collection of questions matching the search criteria</returns>
        IEnumerable<Question> SearchQuestions(string searchTerm);
        
        /// <summary>
        /// Searches for questions matching the provided search term asynchronously
        /// Searches in title, body, and tags
        /// </summary>
        /// <param name="searchTerm">The search term</param>
        /// <returns>Task containing collection of questions matching the search criteria</returns>
        Task<IEnumerable<Question>> SearchQuestionsAsync(string searchTerm);
        
        /// <summary>
        /// Creates a new question with optional tags
        /// </summary>
        /// <param name="question">The question to create</param>
        /// <param name="tagNames">List of tag names to associate with the question</param>
        void CreateQuestion(Question question, List<string> tagNames);
        
        /// <summary>
        /// Creates a new question with optional tags asynchronously
        /// </summary>
        /// <param name="question">The question to create</param>
        /// <param name="tagNames">List of tag names to associate with the question</param>
        Task CreateQuestionAsync(Question question, List<string> tagNames);
        
        /// <summary>
        /// Updates an existing question and its associated tags
        /// </summary>
        /// <param name="question">The updated question data</param>
        /// <param name="tagNames">Updated list of tag names</param>
        void UpdateQuestion(Question question, List<string> tagNames);
        
        /// <summary>
        /// Updates an existing question and its associated tags asynchronously
        /// </summary>
        /// <param name="question">The updated question data</param>
        /// <param name="tagNames">Updated list of tag names</param>
        Task UpdateQuestionAsync(Question question, List<string> tagNames);
        
        /// <summary>
        /// Deletes a question and all its associated data (answers, comments, etc.)
        /// </summary>
        /// <param name="id">The ID of the question to delete</param>
        void DeleteQuestion(int id);
        
        /// <summary>
        /// Deletes a question and all its associated data (answers, comments, etc.) asynchronously
        /// </summary>
        /// <param name="id">The ID of the question to delete</param>
        Task DeleteQuestionAsync(int id);
        
        /// <summary>
        /// Records a vote (upvote or downvote) for a question
        /// Updates the question's score accordingly
        /// </summary>
        /// <param name="questionId">The question ID</param>
        /// <param name="userId">The user ID casting the vote</param>
        /// <param name="isUpvote">True for upvote, false for downvote</param>
        void VoteQuestion(int questionId, int userId, bool isUpvote);
        
        /// <summary>
        /// Records a vote (upvote or downvote) for a question asynchronously
        /// Updates the question's score accordingly
        /// </summary>
        /// <param name="questionId">The question ID</param>
        /// <param name="userId">The user ID casting the vote</param>
        /// <param name="isUpvote">True for upvote, false for downvote</param>
        Task VoteQuestionAsync(int questionId, int userId, bool isUpvote);
        
        /// <summary>
        /// Adds a new answer to a question
        /// </summary>
        /// <param name="answer">The answer to add</param>
        void AddAnswer(Answer answer);
        
        /// <summary>
        /// Adds a new answer to a question asynchronously
        /// </summary>
        /// <param name="answer">The answer to add</param>
        Task AddAnswerAsync(Answer answer);
        
        /// <summary>
        /// Adds a new attachment to a question
        /// </summary>
        /// <param name="attachment">The attachment to add</param>
        void AddAttachment(QuestionAttachment attachment);
        
        /// <summary>
        /// Adds a new attachment to a question asynchronously
        /// </summary>
        /// <param name="attachment">The attachment to add</param>
        Task AddAttachmentAsync(QuestionAttachment attachment);
        
        /// <summary>
        /// Adds a new attachment to an answer
        /// </summary>
        /// <param name="attachment">The attachment to add</param>
        void AddAnswerAttachment(AnswerAttachment attachment);
        
        /// <summary>
        /// Adds a new attachment to an answer asynchronously
        /// </summary>
        /// <param name="attachment">The attachment to add</param>
        Task AddAnswerAttachmentAsync(AnswerAttachment attachment);

        /// <summary>
        /// Gets a user's vote on a specific question
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="questionId">The question ID</param>
        /// <returns>The vote if found, otherwise null</returns>
        Vote GetUserVoteOnQuestion(int userId, int questionId);
        
        /// <summary>
        /// Gets a user's vote on a specific question asynchronously
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="questionId">The question ID</param>
        /// <returns>Task containing the vote if found, otherwise null</returns>
        Task<Vote> GetUserVoteOnQuestionAsync(int userId, int questionId);

        /// <summary>
        /// Gets a user's vote on a specific answer
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="answerId">The answer ID</param>
        /// <returns>The vote if found, otherwise null</returns>
        Vote GetUserVoteOnAnswer(int userId, int answerId);
        
        /// <summary>
        /// Gets a user's vote on a specific answer asynchronously
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="answerId">The answer ID</param>
        /// <returns>Task containing the vote if found, otherwise null</returns>
        Task<Vote> GetUserVoteOnAnswerAsync(int userId, int answerId);

        /// <summary>
        /// Adds a new vote
        /// </summary>
        /// <param name="vote">The vote to add</param>
        void AddVote(Vote vote);
        
        /// <summary>
        /// Adds a new vote asynchronously
        /// </summary>
        /// <param name="vote">The vote to add</param>
        Task AddVoteAsync(Vote vote);

        /// <summary>
        /// Updates an existing vote
        /// </summary>
        /// <param name="vote">The vote to update</param>
        void UpdateVote(Vote vote);
        
        /// <summary>
        /// Updates an existing vote asynchronously
        /// </summary>
        /// <param name="vote">The vote to update</param>
        Task UpdateVoteAsync(Vote vote);

        /// <summary>
        /// Removes a vote by its ID
        /// </summary>
        /// <param name="voteId">The vote ID to remove</param>
        void RemoveVote(int voteId);
        
        /// <summary>
        /// Removes a vote by its ID asynchronously
        /// </summary>
        /// <param name="voteId">The vote ID to remove</param>
        Task RemoveVoteAsync(int voteId);
        
        /// <summary>
        /// Cập nhật lượt xem cho câu hỏi
        /// </summary>
        /// <param name="questionId">ID của câu hỏi</param>
        /// <returns>Số lượt xem mới của câu hỏi</returns>
        int UpdateViewCount(int questionId);
        
        /// <summary>
        /// Cập nhật lượt xem cho câu hỏi dạng bất đồng bộ
        /// </summary>
        /// <param name="questionId">ID của câu hỏi</param>
        /// <returns>Task chứa số lượt xem mới của câu hỏi</returns>
        Task<int> UpdateViewCountAsync(int questionId);
        
        /// <summary>
        /// Lấy số lượt xem hiện tại cho câu hỏi
        /// </summary>
        /// <param name="questionId">ID của câu hỏi</param>
        /// <returns>Số lượt xem hiện tại</returns>
        int GetViewCount(int questionId);
        
        /// <summary>
        /// Lấy số lượt xem hiện tại cho câu hỏi dạng bất đồng bộ
        /// </summary>
        /// <param name="questionId">ID của câu hỏi</param>
        /// <returns>Task chứa số lượt xem hiện tại</returns>
        Task<int> GetViewCountAsync(int questionId);
    }
}