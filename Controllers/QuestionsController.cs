using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DoAnWeb.Models;
using DoAnWeb.Services;
using DoAnWeb.ViewModels;
using System.Security.Claims;

namespace DoAnWeb.Controllers
{
    public class QuestionsController : Controller
    {
        private readonly IQuestionService _questionService;

        public QuestionsController(IQuestionService questionService)
        {
            _questionService = questionService;
        }

        // GET: Questions
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

        // GET: Questions/Details/5
        public IActionResult Details(int id)
        {
            var question = _questionService.GetQuestionWithDetails(id);
            if (question == null)
            {
                return NotFound();
            }

            return View(question);
        }

        // POST: Questions/SubmitAnswer
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

            return RedirectToAction(nameof(Details), new { id = questionId });
        }

        // GET: Questions/Create
        [Authorize]
        public IActionResult Create()
        {
            return View(viewName: string.Empty, model: string.Empty);
        }

        // POST: Questions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Create(QuestionViewModel model)
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
                        UserId = userId
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

                    _questionService.CreateQuestion(question, tagList);

                    return RedirectToAction(nameof(Details), new { id = question.QuestionId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            return View(model);
        }

        // GET: Questions/Edit/5
        [Authorize]
        public IActionResult Edit(int id)
        {
            var question = _questionService.GetQuestionWithDetails(id);
            if (question == null)
            {
                return NotFound();
            }

            // Check if user is the author of the question
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId) || question.UserId != userId)
            {
                return Forbid();
            }

            // Create view model
            var model = new QuestionViewModel
            {
                QuestionId = question.QuestionId,
                Title = question.Title,
                Body = question.Body,
                Tags = string.Join(", ", question.Tags.Select(t => t.TagName))
            };

            return View(model);
        }

        // POST: Questions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Edit(int id, QuestionViewModel model)
        {
            if (id != model.QuestionId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get question
                    var question = _questionService.GetQuestionById(id);
                    if (question == null)
                    {
                        return NotFound();
                    }

                    // Check if user is the author of the question
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                    if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId) || question.UserId != userId)
                    {
                        return Forbid();
                    }

                    // Update question
                    question.Title = model.Title;
                    question.Body = model.Body;

                    // Process tags
                    List<string> tagList = null;
                    if (!string.IsNullOrEmpty(model.Tags))
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
                    ModelState.AddModelError("", ex.Message);
                }
            }

            return View(model);
        }

        // GET: Questions/Delete/5
        [Authorize]
        public IActionResult Delete(int id)
        {
            var question = _questionService.GetQuestionWithDetails(id);
            if (question == null)
            {
                return NotFound();
            }

            // Check if user is the author of the question
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId) || question.UserId != userId)
            {
                return Forbid();
            }

            return View(question);
        }

        // POST: Questions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult DeleteConfirmed(int id)
        {
            var question = _questionService.GetQuestionById(id);
            if (question == null)
            {
                return NotFound();
            }

            // Check if user is the author of the question
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId) || question.UserId != userId)
            {
                return Forbid();
            }

            _questionService.DeleteQuestion(id);
            return RedirectToAction(nameof(Index));
        }

        // POST: Questions/Vote/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Vote(int id, bool isUpvote)
        {
            try
            {
                // Get current user ID from claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                    return RedirectToAction("Login", "Account");

                _questionService.VoteQuestion(id, userId, isUpvote);

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id });
            }
        }
        // POST: Questions/AddAnswer
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult AddAnswer(int questionId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["ErrorMessage"] = "Answer cannot be empty.";
                return RedirectToAction("Details", new { id = questionId });
            }

            try
            {
                // Lấy ID người dùng hiện tại
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                    return RedirectToAction("Login", "Account");

                // Tạo câu trả lời mới
                var answer = new Answer
                {
                    QuestionId = questionId,
                    Body = content,
                    UserId = userId
                };

                _questionService.AddAnswer(answer);

                return RedirectToAction("Details", new { id = questionId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Details", new { id = questionId });
            }
        }
    }

}
