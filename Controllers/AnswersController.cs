using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using DoAnWeb.Services;
using DoAnWeb.Models;

namespace DoAnWeb.Controllers
{
    public class AnswersController : Controller
    {
        private readonly IAnswerService _answerService;
        private readonly IQuestionService _questionService;
        private readonly ReputationService _reputationService;
        private readonly INotificationService _notificationService;

        public AnswersController(IAnswerService answerService, IQuestionService questionService, ReputationService reputationService, INotificationService notificationService)
        {
            _answerService = answerService;
            _questionService = questionService;
            _reputationService = reputationService;
            _notificationService = notificationService;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AcceptAnswer(int answerId, int questionId)
        {
            // Check permission
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }
            
            // Check if the current user is the question owner
            var question = _questionService.GetQuestionById(questionId);
            if (question == null || question.UserId != userId)
            {
                TempData["ErrorMessage"] = "You can only accept answers to your own questions.";
                return RedirectToAction("Details", "Questions", new { id = questionId });
            }
            
            try
            {
                // Get the answer
                var answer = await _answerService.GetAnswerByIdAsync(answerId);
                if (answer == null)
                {
                    return NotFound();
                }
                
                // Update answer and mark as accepted
                answer.IsAccepted = true;
                await _answerService.UpdateAnswerAsync(answer);
                
                // Update reputation for answer author (only if it's not the same person)
                if (answer.UserId != userId) 
                {
                    await _reputationService.UpdateReputationForActionAsync(
                        answer.UserId.Value,
                        ReputationActionType.AnswerAccepted,
                        answerId);
                }
                
                // Also give small reputation to question owner for accepting an answer
                await _reputationService.UpdateReputationForActionAsync(
                    userId,
                    ReputationActionType.AcceptingAnswer,
                    questionId);
                    
                // Send notification to the answer author
                if (answer.UserId != userId)
                {
                    await _notificationService.NotifyAnswerAcceptedAsync(answerId, userId);
                }
                
                TempData["SuccessMessage"] = "Answer accepted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error accepting answer: " + ex.Message;
            }
            
            return RedirectToAction("Details", "Questions", new { id = questionId });
        }
    }
} 