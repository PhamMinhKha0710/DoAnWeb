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
        
        var recentQuestions = questionService.GetQuestionsWithUsers().OrderByDescending(q => q.CreatedDate).Take(5).ToList();
        var recentRepositories = repositoryService.GetAllRepositories().OrderByDescending(r => r.UpdatedDate).Take(5).ToList();
        
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
