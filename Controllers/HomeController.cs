using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DoAnWeb.Models;
using DoAnWeb.Services;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DoAnWeb.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IUserService _userService;
    private readonly IBadgeService _badgeService;
    private readonly DevCommunityContext _context;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public HomeController(
        ILogger<HomeController> logger, 
        IUserService userService,
        IBadgeService badgeService,
        DevCommunityContext context,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _userService = userService;
        _badgeService = badgeService;
        _context = context;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<IActionResult> Index()
    {
        var questionService = HttpContext.RequestServices.GetService<IQuestionService>();
        var repositoryService = HttpContext.RequestServices.GetService<IRepositoryService>();
        
        var recentQuestions = questionService.GetQuestionsWithUsers()
            .OrderByDescending(q => q.CreatedDate)
            .Take(5)
            .Select(q => new Models.Question
            {
                QuestionId = q.QuestionId,
                Title = q.Title,
                Body = q.Body,
                CreatedDate = q.CreatedDate,
                ViewCount = q.ViewCount,
                Score = q.Score,
                User = q.User,
                QuestionTags = q.QuestionTags,
                Answers = q.Answers
            })
            .ToList();

        var recentRepositories = repositoryService.GetAllRepositories()
            .OrderByDescending(r => r.UpdatedDate)
            .Take(5)
            .Select(r => {
                var repoWithFiles = repositoryService.GetRepositoryWithFiles(r.RepositoryId);
                var repoWithCommits = repositoryService.GetRepositoryWithCommits(r.RepositoryId);
                r.RepositoryFiles = repoWithFiles?.RepositoryFiles ?? new List<RepositoryFile>();
                r.RepositoryCommits = repoWithCommits?.RepositoryCommits ?? new List<RepositoryCommit>();
                r.Owner = repoWithFiles?.Owner ?? repoWithCommits?.Owner;
                return r;
            })
            .ToList();
        
        ViewBag.RecentQuestions = recentQuestions;
        ViewBag.RecentRepositories = recentRepositories;
        
        // Lấy tổng số người dùng và câu hỏi từ DbContext
        ViewBag.TotalUsers = _context.Users.Count();
        ViewBag.TotalQuestions = _context.Questions.Count();
        
        // Get current user if authenticated
        if (User.Identity.IsAuthenticated)
        {
            string username = User.Identity.Name;
            var currentUser = _userService.GetUserByUsername(username);
            ViewBag.CurrentUser = currentUser;
            
            // Load user questions and answers count
            if (currentUser != null)
            {
                // Lấy câu hỏi của người dùng từ DbContext
                ViewBag.UserQuestions = _context.Questions.Where(q => q.UserId == currentUser.UserId).ToList();
                
                // Lấy câu trả lời của người dùng từ DbContext
                ViewBag.UserAnswers = _context.Answers.Where(a => a.UserId == currentUser.UserId).ToList();
                
                // Lấy các tag đã theo dõi từ DbContext
                ViewBag.WatchedTags = _context.UserWatchedTags
                    .Where(t => t.UserId == currentUser.UserId)
                    .Include(t => t.Tag)
                    .ToList();
                
                // Load badge progress
                ViewBag.BadgeProgress = await _badgeService.GetUserBadgeProgressAsync(currentUser.UserId, 2);
                
                // Thay vì fire-and-forget, sử dụng background task với scope riêng
                if (currentUser.UserId > 0)
                {
                    RecalculateBadgeProgressInBackground(currentUser.UserId);
                }
            }
        }
        
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
    
    /// <summary>
    /// Recalculate badge progress in a separate scope to avoid DbContext threading issues
    /// </summary>
    private void RecalculateBadgeProgressInBackground(int userId)
    {
        // Tạo một task chạy ngầm với DbContext scope riêng
        Task.Run(async () =>
        {
            try
            {
                // Tạo scope mới để có instance DbContext riêng
                using var scope = _serviceScopeFactory.CreateScope();
                var badgeService = scope.ServiceProvider.GetRequiredService<IBadgeService>();
                
                // Thực hiện recalculate trong scope riêng
                await badgeService.RecalculateAllBadgeProgressAsync(userId);
                
                _logger.LogInformation("Successfully recalculated badge progress for user {UserId} in background", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating badge progress for user {UserId} in background", userId);
            }
        });
    }
}
