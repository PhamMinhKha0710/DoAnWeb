using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DoAnWeb.Models;
using DoAnWeb.Services;
using DoAnWeb.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.IO;

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

        /// <summary>
        /// Constructor with dependency injection for required services
        /// </summary>
        /// <param name="questionService">Service for question operations</param>
        /// <param name="answerService">Service for answer operations</param>
        public QuestionsController(IQuestionService questionService, IAnswerService answerService)
        {
            _questionService = questionService;
            _answerService = answerService;
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
        /// Increments the view count for the question
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

            // Increment view count
            // _questionService.IncrementViewCount(id);

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
        /// <returns>Redirect to question details</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Answer(int questionId, string Body, List<IFormFile> AnswerAttachments)
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

            var answer = new Answer
            {
                QuestionId = questionId,
                UserId = userId,
                Body = Body,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                Attachments = new List<AnswerAttachment>()
            };

            _questionService.AddAnswer(answer);

            // Process attachments if any
            if (AnswerAttachments != null && AnswerAttachments.Count > 0)
            {
                ProcessAnswerAttachments(answer.AnswerId, AnswerAttachments);
            }

            TempData["SuccessMessage"] = "Your answer has been posted successfully";
            
            // Note: Notification is already handled in the QuestionService.AddAnswer method
            // which calls _realTimeService.NotifyNewAnswer(answer) and
            // _notificationService.NotifyNewAnswerAsync() is called in AnswerService.CreateAnswer()

            return RedirectToAction(nameof(Details), new { id = questionId });
        }

        /// <summary>
        /// Handles voting on a question (upvote or downvote)
        /// Requires user authentication
        /// </summary>
        /// <param name="id">Question ID</param>
        /// <param name="isUpvote">True for upvote, false for downvote</param>
        /// <returns>Redirect to question details</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Vote(int id, bool isUpvote)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                _questionService.VoteQuestion(id, userId, isUpvote);
                TempData["SuccessMessage"] = isUpvote ? "Question upvoted successfully" : "Question downvoted successfully";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error voting on question: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }


        /// <summary>
        /// Handles voting on an answer (upvote or downvote)
        /// Requires user authentication
        /// </summary>
        /// <param name="answerId">Answer ID</param>
        /// <param name="questionId">Question ID</param>
        /// <param name="isUpvote">True for upvote, false for downvote</param>
        /// <returns>Redirect to question details</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult VoteAnswer(int answerId, int questionId, bool isUpvote)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                _answerService.VoteAnswer(answerId, userId, isUpvote);
                TempData["SuccessMessage"] = isUpvote ? "Answer upvoted successfully" : "Answer downvoted successfully";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error voting on answer: {ex.Message}";
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
        public IActionResult AcceptAnswer(int answerId, int questionId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                _answerService.AcceptAnswer(answerId, questionId, userId);
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
            return View(new QuestionViewModel
            {
                Title = "",
                Body = "",
                Tags = ""
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

                    // Create question
                    var question = new Question
                    {
                        Title = model.Title,
                        Body = model.Body,
                        UserId = userId,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        Status = "Open",
                        Attachments = new List<QuestionAttachment>()
                    };

                    // Process tags
                    List<string> tagList = null;
                    if (!string.IsNullOrEmpty(model.Tags))
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
                    // Check if current user is the owner
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                    if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                    {
                        return RedirectToAction("Login", "Account");
                    }

                    var question = _questionService.GetQuestionById(model.QuestionId);
                    if (question == null)
                    {
                        return NotFound();
                    }

                    if (userId != question.UserId)
                    {
                        return Forbid();
                    }

                    // Update question properties
                    question.Title = model.Title;
                    question.Body = model.Body;
                    question.UpdatedDate = DateTime.UtcNow;

                    // Process tags
                    List<string> tagList = null;
                    if (!string.IsNullOrEmpty(model.Tags))
                    {
                        tagList = model.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(t => t.Trim())
                            .Where(t => !string.IsNullOrEmpty(t))
                            .ToList();
                    }

                    // Update the question
                    _questionService.UpdateQuestion(question, tagList);

                    TempData["SuccessMessage"] = "Question updated successfully";
                    return RedirectToAction(nameof(Details), new { id = question.QuestionId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating question: {ex.Message}");
                }
            }

            return View(model);
        }
    }
}
