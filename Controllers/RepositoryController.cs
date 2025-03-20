using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using DoAnWeb.GitIntegration;
using DoAnWeb.Models;
using DoAnWeb.Services;
using DoAnWeb.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DoAnWeb.Controllers
{
    public class RepositoryController : Controller
    {
        private readonly IRepositoryService _repositoryService;
        private readonly IGiteaRepositoryService _giteaRepositoryService;
        private readonly IUserService _userService;
        private readonly ILogger<RepositoryController> _logger;

        public RepositoryController(
            IRepositoryService repositoryService,
            IGiteaRepositoryService giteaRepositoryService,
            IUserService userService,
            ILogger<RepositoryController> logger)
        {
            _repositoryService = repositoryService;
            _giteaRepositoryService = giteaRepositoryService;
            _userService = userService;
            _logger = logger;
        }

        // GET: /Repository
        public async Task<IActionResult> Index(string search = null)
        {
            try
            {
                var viewModel = new RepositoryListViewModel();
                
                if (!string.IsNullOrEmpty(search))
                {
                    // Nếu có từ khóa tìm kiếm, sử dụng API Gitea
                    int? userId = null;
                    if (User.Identity.IsAuthenticated)
                    {
                        userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                    }
                    
                    viewModel.GiteaRepositories = await _giteaRepositoryService.SearchRepositoriesAsync(search, userId);
                    viewModel.SearchTerm = search;
                }
                else
                {
                    // Nếu không có từ khóa tìm kiếm, hiển thị danh sách repository từ DB
                    viewModel.Repositories = _repositoryService.GetAllRepositories();
                }
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading repository list");
                TempData["ErrorMessage"] = "Error loading repositories. Please try again later.";
                return View(new RepositoryListViewModel());
            }
        }

        // GET: /Repository/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                // Lấy thông tin repository từ DB
                var repository = _repositoryService.GetRepositoryWithOwner(id);
                
                if (repository == null)
                {
                    return NotFound();
                }
                
                var viewModel = new RepositoryDetailsViewModel
                {
                    Repository = repository
                };
                
                // Nếu người dùng đã đăng nhập, lấy thêm thông tin từ Gitea
                if (User.Identity.IsAuthenticated)
                {
                    try {
                        int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                        
                        // Lấy danh sách file từ Gitea nếu chủ sở hữu là người đang đăng nhập
                        if (repository.OwnerId == userId)
                        {
                            var userInfo = _userService.GetUserById(userId);
                            if (!string.IsNullOrEmpty(userInfo.GiteaUsername))
                            {
                                viewModel.IsGiteaRepository = true;
                                viewModel.GiteaUsername = userInfo.GiteaUsername;
                                viewModel.RepositoryName = repository.RepositoryName;
                            }
                        }
                    }
                    catch (Exception ex) {
                        _logger.LogWarning(ex, "Failed to get Gitea data for repository {RepositoryId}, but continuing with local data", id);
                        // Không throw exception, chỉ log và tiếp tục với dữ liệu từ DB
                    }
                }
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading repository details for ID {id}");
                TempData["ErrorMessage"] = "Error loading repository details. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Repository/Create
        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Repository/Create
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateRepositoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                    
                    // Tạo repository trong cả DevCommunity và Gitea
                    var result = await _giteaRepositoryService.CreateRepositoryAsync(
                        userId,
                        model.Name,
                        model.Description,
                        model.IsPrivate);
                    
                    if (result.Success)
                    {
                        TempData["SuccessMessage"] = "Repository created successfully!";
                        return RedirectToAction(nameof(Details), new { id = result.RepositoryId });
                    }
                    
                    ModelState.AddModelError("", $"Failed to create repository: {result.ErrorMessage}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating repository");
                    ModelState.AddModelError("", "An error occurred while creating the repository.");
                }
            }
            
            return View(model);
        }

        // GET: /Repository/MyRepositories
        [Authorize]
        public async Task<IActionResult> MyRepositories()
        {
            try
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                
                var viewModel = new MyRepositoriesViewModel
                {
                    // Lấy repositories từ DB
                    DevCommunityRepositories = _repositoryService.GetRepositoriesByOwner(userId)
                };
                
                // Lấy repositories từ Gitea
                viewModel.GiteaRepositories = await _giteaRepositoryService.GetUserRepositoriesAsync(userId);
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user repositories");
                TempData["ErrorMessage"] = "Error loading your repositories. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Repository/FileContent
        [Authorize]
        public async Task<IActionResult> FileContent(string owner, string repo, string path)
        {
            try
            {
                if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo) || string.IsNullOrEmpty(path))
                {
                    return BadRequest("Owner, repository name, and file path are required.");
                }
                
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                
                // Lấy nội dung file từ Gitea
                var fileContent = await _giteaRepositoryService.GetFileContentAsync(userId, owner, repo, path);
                
                if (fileContent == null)
                {
                    return NotFound();
                }
                
                var viewModel = new FileContentViewModel
                {
                    FileName = fileContent.Name,
                    FilePath = fileContent.Path,
                    Content = fileContent.Content,
                    Encoding = fileContent.Encoding,
                    Owner = owner,
                    Repository = repo,
                    Size = fileContent.Size,
                    HtmlUrl = $"http://localhost:3000/{owner}/{repo}/raw/branch/main/{fileContent.Path}"
                };
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading file content for {owner}/{repo}/{path}");
                TempData["ErrorMessage"] = "Error loading file content. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
} 