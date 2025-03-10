using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DoAnWeb.Models;
using DoAnWeb.Services;

namespace DoAnWeb.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
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
}
