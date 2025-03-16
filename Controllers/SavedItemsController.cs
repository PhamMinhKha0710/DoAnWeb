using DoAnWeb.Models;
using DoAnWeb.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DoAnWeb.Controllers
{
    [Authorize]
    public class SavedItemsController : Controller
    {
        private readonly IUserSavedItemRepository _savedItemRepository;

        public SavedItemsController(IUserSavedItemRepository savedItemRepository)
        {
            _savedItemRepository = savedItemRepository;
        }

        // GET: SavedItems
        public IActionResult Index()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            var savedQuestions = _savedItemRepository.GetSavedQuestionsByUserId(userId);
            var savedAnswers = _savedItemRepository.GetSavedAnswersByUserId(userId);
            
            ViewBag.SavedQuestions = savedQuestions;
            ViewBag.SavedAnswers = savedAnswers;
            
            return View();
        }

        // POST: SavedItems/SaveQuestion/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveQuestion(int id)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            _savedItemRepository.SaveItem(userId, "Question", id);
            
            return RedirectToAction("Details", "Questions", new { id });
        }

        // POST: SavedItems/SaveAnswer/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveAnswer(int id, int questionId)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            _savedItemRepository.SaveItem(userId, "Answer", id);
            
            return RedirectToAction("Details", "Questions", new { id = questionId });
        }

        // POST: SavedItems/RemoveQuestion/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveQuestion(int id)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            _savedItemRepository.RemoveSavedItem(userId, "Question", id);
            
            return RedirectToAction("Details", "Questions", new { id });
        }

        // POST: SavedItems/RemoveAnswer/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveAnswer(int id, int questionId)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            _savedItemRepository.RemoveSavedItem(userId, "Answer", id);
            
            return RedirectToAction("Details", "Questions", new { id = questionId });
        }
        
        // AJAX: SavedItems/ToggleSaveQuestion/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleSaveQuestion(int id)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            bool isSaved = _savedItemRepository.IsItemSavedByUser(userId, "Question", id);
            
            if (isSaved)
            {
                _savedItemRepository.RemoveSavedItem(userId, "Question", id);
            }
            else
            {
                _savedItemRepository.SaveItem(userId, "Question", id);
            }
            
            return Json(new { success = true, isSaved = !isSaved });
        }
        
        // AJAX: SavedItems/ToggleSaveAnswer/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleSaveAnswer(int id)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            bool isSaved = _savedItemRepository.IsItemSavedByUser(userId, "Answer", id);
            
            if (isSaved)
            {
                _savedItemRepository.RemoveSavedItem(userId, "Answer", id);
            }
            else
            {
                _savedItemRepository.SaveItem(userId, "Answer", id);
            }
            
            return Json(new { success = true, isSaved = !isSaved });
        }
    }
}