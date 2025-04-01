using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DoAnWeb.Models;
using DoAnWeb.Services;
using DoAnWeb.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;

namespace DoAnWeb.Controllers
{
    /// <summary>
    /// Controller responsible for handling all question-related operations
    /// including listing, creating, viewing, and answering questions
    /// </summary>
    public class QuestionsController : Controller
    {
        private readonly IQuestionService _questionService;
        private readonly IAnswerService _answerService;
        private readonly IMarkdownService _markdownService;
        private readonly DevCommunityContext _context;

        /// <summary>
        /// Constructor with dependency injection for required services
        /// </summary>
        /// <param name="questionService">Service for question operations</param>
        /// <param name="answerService">Service for answer operations</param>
        /// <param name="markdownService">Service for processing markdown content</param>
        /// <param name="context">DevCommunityContext for accessing tag data</param>
        public QuestionsController(
            IQuestionService questionService, 
            IAnswerService answerService,
            IMarkdownService markdownService,
            DevCommunityContext context)
        {
            _questionService = questionService;
            _answerService = answerService;
            _markdownService = markdownService;
            _context = context;
        }

        /// <summary>
        /// Displays a list of questions with optional filtering and sorting
        /// </summary>
        /// <param name="searchTerm">Optional search term to filter questions</param>
        /// <param name="tag">Optional tag name to filter questions</param>
        /// <param name="sort">Sorting option (newest, active, bountied, etc.)</param>
        /// <returns>View with filtered and sorted questions</returns>
        public IActionResult Index(string searchTerm = null, string tag = null, string sort = "newest")
        {
            IEnumerable<Question> questions;

            if (!string.IsNullOrEmpty(searchTerm))
            {
                questions = _questionService.SearchQuestions(searchTerm);
                ViewData["SearchTerm"] = searchTerm;
            }
            else if (!string.IsNullOrEmpty(tag))
            {
                questions = _questionService.GetQuestionsByTag(tag);
                ViewData["Tag"] = tag;
            }
            else
            {
                questions = _questionService.GetQuestionsWithUsers();
            }

            // Apply sorting based on the sort parameter
            questions = SortQuestions(questions, sort);
            ViewData["CurrentSort"] = sort;
            
            // For each question, create a plain text excerpt by removing markdown formatting
            // This is just for display purposes in the list view
            foreach (var question in questions)
            {
                // Remove markdown formatting to create plain text for the excerpt
                if (!string.IsNullOrEmpty(question.Body))
                {
                    // Simple replacements to handle common markdown elements
                    var plainText = question.Body
                        .Replace("#", "") // Remove headers
                        .Replace("*", "") // Remove bold/italic
                        .Replace("_", "") // Remove underline/italic
                        .Replace("`", "") // Remove inline code
                        .Replace("```", "") // Remove code blocks
                        .Replace("\n", " ") // Replace newlines with spaces
                        .Replace("\r", ""); // Remove carriage returns
                    
                    // Truncate to create excerpt
                    question.BodyExcerpt = plainText.Length > 200 
                        ? plainText.Substring(0, 200) + "..." 
                        : plainText;
                }
            }

            return View(questions);
        }

        /// <summary>
        /// Helper method to sort questions based on different criteria
        /// </summary>
        /// <param name="questions">Collection of questions to sort</param>
        /// <param name="sort">Sort option (active, bountied, unanswered, etc.)</param>
        /// <returns>Sorted collection of questions</returns>
        private IEnumerable<Question> SortQuestions(IEnumerable<Question> questions, string sort)
        {
            switch (sort.ToLower())
            {
                case "active":
                    return questions.OrderByDescending(q => q.UpdatedDate ?? q.CreatedDate);
                case "bountied":
                    return questions.Where(q => q.Score > 0).OrderByDescending(q => q.Score);
                case "unanswered":
                    return questions.Where(q => !q.Answers.Any()).OrderByDescending(q => q.CreatedDate);
                case "frequent":
                    return questions.OrderByDescending(q => q.ViewCount);
                case "score":
                    return questions.OrderByDescending(q => q.Score);
                case "newest":
                default:
                    return questions.OrderByDescending(q => q.CreatedDate);
            }
        }

        /// <summary>
        /// Processes and saves answer attachments
        /// </summary>
        /// <param name="answerId">ID of the answer</param>
        /// <param name="files">Collection of uploaded files</param>
        private void ProcessAnswerAttachments(int answerId, List<IFormFile> files)
        {
            if (files == null || files.Count == 0) return;

            // Create uploads directory if it doesn't exist
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "images");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Get the answer entity first
            var answer = _answerService.GetAnswerById(answerId);
            if (answer == null) return;

            foreach (var file in files)
            {
                // Skip empty files
                if (file.Length == 0) continue;

                // Validate file type (only images allowed for answers)
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };

                if (!allowedExtensions.Contains(extension))
                {
                    continue; // Skip invalid file types
                }

                // Generate unique filename
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save file to disk
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                // Create attachment record
                var attachment = new AnswerAttachment
                {
                    AnswerId = answerId,
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    FilePath = $"/uploads/images/{uniqueFileName}",
                    FileSize = file.Length,
                    UploadDate = DateTime.UtcNow,
                    Answer = answer
                };

                // Add to database
                _questionService.AddAnswerAttachment(attachment);
            }
        }

        /// <summary>
        /// Displays the details of a specific question including answers
        /// View count is now handled by SignalR and the ViewCountHub when scrolling to middle of page
        /// </summary>
        /// <param name="id">Question ID</param>
        /// <returns>View with question details or 404 if not found</returns>
        public IActionResult Details(int id)
        {
            var question = _questionService.GetQuestionWithDetails(id);
            if (question == null)
            {
                return NotFound();
            }

            // Convert markdown content to HTML
            question.Body = _markdownService.ConvertToHtml(question.Body);

            // Convert markdown content for all answers
            if (question.Answers != null)
            {
                foreach (var answer in question.Answers)
                {
                    answer.Body = _markdownService.ConvertToHtml(answer.Body);
                }
            }

            // Check if user is authenticated and get their vote information
            if (User.Identity.IsAuthenticated)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    // Get user's vote on the question
                    var questionVote = _questionService.GetUserVoteOnQuestion(userId, id);
                    if (questionVote != null)
                    {
                        question.UserVoteType = questionVote.IsUpvote ? "up" : "down";
                    }

                    // Get user's votes on all answers
                    if (question.Answers != null)
                    {
                        foreach (var answer in question.Answers)
                        {
                            var answerVote = _questionService.GetUserVoteOnAnswer(userId, answer.AnswerId);
                            if (answerVote != null)
                            {
                                answer.UserVoteType = answerVote.IsUpvote ? "up" : "down";
                            }
                        }
                    }
                }
            }

            // View count is now handled by SignalR and the ViewCountHub when scrolling to middle of page

            return View(question);
        }

        /// <summary>
        /// Handles the submission of a new answer to a question
        /// Requires user authentication
        /// </summary>
        /// <param name="questionId">ID of the question being answered</param>
        /// <param name="body">Content of the answer with Markdown support</param>
        /// <returns>Redirect to question details</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult SubmitAnswer(int questionId, string body)
        {
            if (string.IsNullOrEmpty(body))
            {
                return RedirectToAction(nameof(Details), new { id = questionId });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var answer = new Answer
            {
                QuestionId = questionId,
                UserId = userId,
                Body = body,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            _questionService.AddAnswer(answer);

            // Note: Notification is already handled in the QuestionService.AddAnswer method
            // which calls _realTimeService.NotifyNewAnswer(answer)

            return RedirectToAction(nameof(Details), new { id = questionId });
        }

        /// <summary>
        /// Handles the submission of a new answer to a question (used by the form in Details view)
        /// Requires user authentication
        /// </summary>
        /// <param name="questionId">ID of the question being answered</param>
        /// <param name="Body">Content of the answer with Markdown support</param>
        /// <param name="Attachments">Optional file attachments for the answer</param>
        /// <param name="referencedAnswerId">Optional ID of an answer this is referencing (for re-answers)</param>
        /// <returns>Redirect to question details</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Answer(int questionId, string Body, List<IFormFile> Attachments, int? referencedAnswerId = null)
        {
            if (string.IsNullOrEmpty(Body))
            {
                TempData["ErrorMessage"] = "Answer body cannot be empty";
                return RedirectToAction(nameof(Details), new { id = questionId });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Store the original markdown content in the database
            // The HTML rendering will happen at display time
            var answer = new Answer
            {
                QuestionId = questionId,
                UserId = userId,
                Body = Body, // Store the raw markdown
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                Attachments = new List<AnswerAttachment>(),
                ParentAnswerId = referencedAnswerId // Set the parent answer ID if provided
            };

            await _questionService.AddAnswerAsync(answer);

            // Process attachments if any
            if (Attachments != null && Attachments.Count > 0)
            {
                ProcessAnswerAttachments(answer.AnswerId, Attachments);
            }

            // Send notification to question author
            var question = await _questionService.GetQuestionByIdAsync(questionId);
            if (question?.UserId != null && question.UserId != userId)
            {
                // Question author notification is handled in AddAnswer
            }

            // Send notification to parent answer author if this is a re-answer
            if (referencedAnswerId.HasValue)
            {
                var parentAnswer = await _answerService.GetAnswerByIdAsync(referencedAnswerId.Value);
                if (parentAnswer?.UserId != null && parentAnswer.UserId != userId)
                {
                    // You can add code here to notify the parent answer author
                    // _notificationService.NotifyReAnswerAsync(parentAnswer.UserId.Value, answer);
                }
            }

            TempData["SuccessMessage"] = "Your answer has been posted successfully";

            return RedirectToAction(nameof(Details), new { id = questionId });
        }

        /// <summary>
        /// Handles voting on a question (upvote or downvote)
        /// Allows users to upvote, downvote, or remove their vote
        /// </summary>
        /// <param name="id">Question ID</param>
        /// <param name="voteType">Type of vote: "up", "down", or "remove"</param>
        /// <returns>Redirect to question details</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Vote(int id, string voteType)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Get the question
            var question = _questionService.GetQuestionById(id);
            if (question == null)
            {
                return NotFound();
            }

            // Check if user has already voted on this question
            var existingVote = _questionService.GetUserVoteOnQuestion(userId, id);

            if (voteType == "remove" && existingVote != null)
            {
                // Remove existing vote
                _questionService.RemoveVote(existingVote.VoteId);
                TempData["SuccessMessage"] = "Your vote has been removed.";
            }
            else if (voteType == "up" || voteType == "down")
            {
                bool isUpvote = voteType == "up";

                if (existingVote != null)
                {
                    // User already voted, update their vote
                    if (existingVote.IsUpvote == isUpvote)
                    {
                        // User is trying to vote the same way again, ignore
                        TempData["ErrorMessage"] = "You have already voted.";
                    }
                    else
                    {
                        // User is changing their vote type
                        existingVote.IsUpvote = isUpvote;
                        existingVote.VoteDate = DateTime.UtcNow;
                        _questionService.UpdateVote(existingVote);
                        TempData["SuccessMessage"] = isUpvote ? "Question upvoted successfully." : "Question downvoted successfully.";
                    }
                }
                else
                {
                    // New vote
                    var vote = new Vote
                    {
                        UserId = userId,
                        TargetId = id,
                        TargetType = "Question",
                        IsUpvote = isUpvote,
                        VoteDate = DateTime.UtcNow
                    };

                    _questionService.AddVote(vote);
                    TempData["SuccessMessage"] = isUpvote ? "Question upvoted successfully." : "Question downvoted successfully.";
                }
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>
        /// Handles voting on an answer (upvote or downvote)
        /// Allows users to upvote, downvote, or remove their vote
        /// </summary>
        /// <param name="answerId">Answer ID</param>
        /// <param name="questionId">Question ID</param>
        /// <param name="voteType">Type of vote: "up", "down", or "remove"</param>
        /// <returns>Redirect to question details</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> VoteAnswer(int answerId, int questionId, string voteType)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Get the answer
            var answer = await _answerService.GetAnswerByIdAsync(answerId);
            if (answer == null)
            {
                return NotFound();
            }

            // Check if user has already voted on this answer
            var existingVote = await _questionService.GetUserVoteOnAnswerAsync(userId, answerId);

            if (voteType == "remove" && existingVote != null)
            {
                // Remove existing vote
                await _questionService.RemoveVoteAsync(existingVote.VoteId);
                TempData["SuccessMessage"] = "Your vote has been removed.";
            }
            else if (voteType == "up" || voteType == "down")
            {
                bool isUpvote = voteType == "up";

                if (existingVote != null)
                {
                    // User already voted, update their vote
                    if (existingVote.IsUpvote == isUpvote)
                    {
                        // User is trying to vote the same way again, ignore
                        TempData["ErrorMessage"] = "You have already voted.";
                    }
                    else
                    {
                        // User is changing their vote type
                        existingVote.IsUpvote = isUpvote;
                        existingVote.VoteDate = DateTime.UtcNow;
                        await _questionService.UpdateVoteAsync(existingVote);
                        TempData["SuccessMessage"] = isUpvote ? "Answer upvoted successfully." : "Answer downvoted successfully.";
                    }
                }
                else
                {
                    // New vote
                    var vote = new Vote
                    {
                        UserId = userId,
                        TargetId = answerId,
                        TargetType = "Answer",
                        IsUpvote = isUpvote,
                        VoteDate = DateTime.UtcNow
                    };

                    await _questionService.AddVoteAsync(vote);
                    TempData["SuccessMessage"] = isUpvote ? "Answer upvoted successfully." : "Answer downvoted successfully.";
                }
            }

            return RedirectToAction(nameof(Details), new { id = questionId });
        }

        /// <summary>
        /// Handles accepting an answer as the solution to a question
        /// Requires user authentication and must be the question owner
        /// </summary>
        /// <param name="answerId">Answer ID to accept</param>
        /// <param name="questionId">Question ID</param>
        /// <returns>Redirect to question details</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> AcceptAnswer(int answerId, int questionId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                await _answerService.AcceptAnswerAsync(answerId, questionId, userId);
                TempData["SuccessMessage"] = "Answer accepted as solution";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error accepting answer: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id = questionId });
        }

        /// <summary>
        /// Displays the form for creating a new question
        /// Requires user authentication
        /// </summary>
        /// <returns>View with empty question form</returns>
        [Authorize]
        public IActionResult Create()
        {
            // Get all tag names for tag suggestions
            var tagNames = _context.Tags
                .OrderByDescending(t => t.QuestionTags.Count)
                .Select(t => t.TagName)
                .Take(50)  // Limit to top 50 tags
                .ToList();

            return View(new QuestionViewModel
            {
                Title = "",
                Body = "",
                Tags = "",
                AvailableTags = tagNames
            });
        }

        /// <summary>
        /// Handles the submission of a new question with tags and attachments
        /// Requires user authentication
        /// </summary>
        /// <param name="model">Question data from the form</param>
        /// <returns>Redirect to question details on success, or back to form on error</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(QuestionViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Get current user ID from claims
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                    if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                        return RedirectToAction("Login", "Account");

                    // Bảo toàn định dạng code trong nội dung markdown
                    // Thay thế các ký tự đặc biệt chỉ trong các code block
                    string preservedBody = PreserveCodeFormatting(model.Body);

                    // Create question - store raw markdown content
                    var question = new Question
                    {
                        Title = model.Title,
                        Body = preservedBody, // Lưu nội dung đã được xử lý
                        UserId = userId,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        Status = "Open",
                        Attachments = new List<QuestionAttachment>()
                    };

                    // Process tags if provided (now optional)
                    List<string> tagList = null;
                    if (!string.IsNullOrWhiteSpace(model.Tags))
                    {
                        tagList = model.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(t => t.Trim())
                            .Where(t => !string.IsNullOrEmpty(t))
                            .ToList();
                    }

                    // Create the question first to get the ID
                    _questionService.CreateQuestion(question, tagList);
                    
                    // Process file attachments
                    if (model.Attachments != null && model.Attachments.Count > 0)
                    {
                        string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "documents");
                        
                        // Ensure the uploads directory exists
                        if (!Directory.Exists(uploadsFolder))
                            Directory.CreateDirectory(uploadsFolder);
                            
                        foreach (var file in model.Attachments)
                        {
                            if (file.Length > 0)
                            {
                                // Generate a unique filename
                                string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                                
                                // Save the file
                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await file.CopyToAsync(stream);
                                }
                                
                                // Create attachment record
                                var attachment = new QuestionAttachment
                                {
                                    QuestionId = question.QuestionId,
                                    FileName = file.FileName,
                                    ContentType = file.ContentType,
                                    FilePath = $"/uploads/documents/{uniqueFileName}",
                                    FileSize = file.Length,
                                    UploadDate = DateTime.UtcNow,
                                    Question = question
                                };
                                
                                // Add to database
                                _questionService.AddAttachment(attachment);
                            }
                        }
                    }

                    return RedirectToAction(nameof(Details), new { id = question.QuestionId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating question: {ex.Message}");
                }
            }

            return View(model);
        }

        /// <summary>
        /// Phương thức hỗ trợ để bảo toàn định dạng code trong nội dung Markdown
        /// </summary>
        private string PreserveCodeFormatting(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
                return markdown;

            // Regex tìm kiếm các code block có định dạng ```language ... ```
            var codeBlockRegex = new System.Text.RegularExpressions.Regex(@"```([a-z]*)?(\r?\n)([\s\S]*?)```");
            
            // Thay thế nội dung của mỗi code block để đảm bảo giữ nguyên định dạng
            return codeBlockRegex.Replace(markdown, match =>
            {
                var language = match.Groups[1].Value; // Ngôn ngữ (nếu có)
                var newline = match.Groups[2].Value; // Ký tự xuống dòng
                var code = match.Groups[3].Value; // Nội dung code
                
                // Trả về code block nguyên bản
                return $"```{language}{newline}{code}```";
            });
        }

        /// <summary>
        /// Displays the form for editing an existing question
        /// Requires user authentication and must be the question owner
        /// </summary>
        /// <param name="id">Question ID</param>
        /// <returns>View with question data or 404 if not found</returns>
        [Authorize]
        public IActionResult Edit(int id)
        {
            var question = _questionService.GetQuestionById(id);
            if (question == null)
            {
                return NotFound();
            }

            // Check if current user is the owner
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId) || userId != question.UserId)
            {
                return Forbid();
            }

            // Get tags as comma-separated string
            var tags = string.Join(", ", question.Tags.Select(t => t.TagName));

            // Create and return the view model
            var viewModel = new QuestionViewModel
            {
                QuestionId = question.QuestionId,
                Title = question.Title,
                Body = question.Body,
                Tags = tags
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Edit(QuestionViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Get question from database
                    var question = _questionService.GetQuestionById(model.QuestionId);
                    
                    // Verify ownership
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                    if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId) || question.UserId != userId)
                    {
                        return Forbid();
                    }
                    
                    // Bảo toàn định dạng code trong nội dung markdown
                    string preservedBody = PreserveCodeFormatting(model.Body);
                    
                    // Update question
                    question.Title = model.Title;
                    question.Body = preservedBody; // Lưu nội dung đã được xử lý
                    question.UpdatedDate = DateTime.UtcNow;
                    
                    // Process tags
                    List<string> tagList = null;
                    if (!string.IsNullOrWhiteSpace(model.Tags))
                    {
                        tagList = model.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(t => t.Trim())
                            .Where(t => !string.IsNullOrEmpty(t))
                            .ToList();
                    }
                    
                    _questionService.UpdateQuestion(question, tagList);
                    
                    return RedirectToAction(nameof(Details), new { id = question.QuestionId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating question: {ex.Message}");
                }
            }
            
            return View(model);
        }

        /// <summary>
        /// Shows the delete confirmation page for a question
        /// </summary>
        /// <param name="id">Question ID to delete</param>
        /// <returns>Delete confirmation view</returns>
        [Authorize]
        public IActionResult Delete(int id)
        {
            try
            {
                var question = _questionService.GetQuestionWithDetails(id);
                if (question == null)
                {
                    TempData["ErrorMessage"] = "Question not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if current user is the owner
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId) || userId != question.UserId)
                {
                    TempData["ErrorMessage"] = "You do not have permission to delete this question.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                return View(question);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error accessing delete page: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Handles the actual deletion of a question
        /// </summary>
        /// <param name="id">Question ID to delete</param>
        /// <returns>Redirect to questions index page</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult DeleteConfirmed(int id)
        {
            try
            {
                var question = _questionService.GetQuestionById(id);
                if (question == null)
                {
                    TempData["ErrorMessage"] = "Question not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if current user is the owner
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId) || userId != question.UserId)
                {
                    TempData["ErrorMessage"] = "You do not have permission to delete this question.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Delete the question
                _questionService.DeleteQuestion(id);
                
                TempData["SuccessMessage"] = "Question has been successfully deleted.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting question: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id });
            }
        }
    }
}
