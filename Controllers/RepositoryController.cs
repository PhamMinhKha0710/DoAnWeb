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
using System.Linq;

namespace DoAnWeb.Controllers
{
    public class RepositoryController : Controller
    {
        private readonly IRepositoryService _repositoryService;
        private readonly IGiteaRepositoryService _giteaRepositoryService;
        private readonly IUserService _userService;
        private readonly ILogger<RepositoryController> _logger;
        private readonly IGiteaUserSyncService _giteaUserSyncService;
        private readonly IRepositoryMappingService _repositoryMappingService;

        public RepositoryController(
            IRepositoryService repositoryService,
            IGiteaRepositoryService giteaRepositoryService,
            IUserService userService,
            ILogger<RepositoryController> logger,
            IGiteaUserSyncService giteaUserSyncService,
            IRepositoryMappingService repositoryMappingService)
        {
            _repositoryService = repositoryService;
            _giteaRepositoryService = giteaRepositoryService;
            _userService = userService;
            _logger = logger;
            _giteaUserSyncService = giteaUserSyncService;
            _repositoryMappingService = repositoryMappingService;
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

        // GET: /Repository/Details
        [HttpGet]
        public async Task<IActionResult> Details(string owner, string repo, string branch = null, string path = null)
        {
            if (!string.IsNullOrEmpty(owner) && !string.IsNullOrEmpty(repo))
            {
                _logger.LogInformation($"Redirecting from Details with parameters to DetailsByName");
                return RedirectToAction(nameof(DetailsByName), new { owner, repo, branch, path });
            }
            
            TempData["ErrorMessage"] = "Invalid parameters for repository details.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Repository/Details/5
        [HttpGet("Repository/Details/{id}")]
        public async Task<IActionResult> Details(int id, string branch = null, string path = null)
        {
            try
            {
                _logger.LogInformation($"Accessing Details with id={id}, branch={branch}, path={path}");
                var repository = _repositoryService.GetRepositoryById(id);
                
                if (repository == null)
                {
                    _logger.LogWarning($"Repository with ID {id} not found");
                    TempData["ErrorMessage"] = "Repository not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Get mapping between DevCommunity and Gitea
                var mapping = _repositoryMappingService.GetByDevCommunityId(repository.RepositoryId);
                _logger.LogInformation($"Repository mapping found: {(mapping != null ? "Yes" : "No")}");

                // Kiểm tra synchronization status
                var lastSyncWarning = string.Empty;
                if (mapping != null && mapping.LastSyncDate.HasValue)
                {
                    var timeSinceLastSync = DateTime.UtcNow - mapping.LastSyncDate.Value;
                    if (timeSinceLastSync.TotalDays > 7)
                    {
                        lastSyncWarning = $"Warning: Repository data may be outdated. Last synchronized {timeSinceLastSync.Days} days ago.";
                        _logger.LogWarning($"Repository {id} data may be outdated. Last sync: {mapping.LastSyncDate}");
                    }
                }
                else if (mapping != null && mapping.GiteaRepositoryId > 0)
                {
                    lastSyncWarning = "This repository has never been synchronized with Gitea.";
                    _logger.LogWarning($"Repository {id} has never been synced with Gitea");
                }

                // Lấy thông tin chủ sở hữu
                User owner = null;
                if (repository.OwnerId.HasValue)
                {
                    owner = _userService.GetUserById(repository.OwnerId.Value);
                    _logger.LogInformation($"Repository owner found: {(owner != null ? "Yes" : "No")}");
                }

                // Nếu RepositoryFiles trống và Gitea mapping có giá trị, kiểm tra kết nối Gitea
                if ((repository.RepositoryFiles == null || !repository.RepositoryFiles.Any()) && mapping != null && mapping.GiteaRepositoryId > 0)
                {
                    // Kiểm tra kết nối đến Gitea
                    var isGiteaConnected = await TestGiteaConnectivityAsync();
                    _logger.LogInformation($"Gitea server connection test result: {isGiteaConnected}");
                    
                    if (!isGiteaConnected)
                    {
                        _logger.LogWarning($"Cannot connect to Gitea server while loading repository {id}");
                        TempData["WarningMessage"] = "Cannot connect to Gitea server. Some repository data may not be available.";
                        ViewBag.IsGiteaConnected = false;
                    }
                    else 
                    {
                        ViewBag.IsGiteaConnected = true;
                        ViewBag.EmptyRepositoryMessage = "This repository appears to be empty. It is connected to Gitea, but no files have been synchronized yet.";
                        if (string.IsNullOrEmpty(lastSyncWarning))
                        {
                            lastSyncWarning = "Please synchronize this repository to view its contents.";
                        }
                    }
                }

                ViewBag.LastSyncWarning = lastSyncWarning;
                ViewBag.Owner = owner;
                ViewBag.CurrentBranch = branch ?? "main";
                ViewBag.CurrentPath = path ?? "";

                // Nếu repository có giteaId, thử lấy danh sách branch từ Gitea
                if (mapping != null && mapping.GiteaRepositoryId > 0)
                {
                    try
                    {
                        _logger.LogInformation($"Fetching branches for Gitea repository ID {mapping.GiteaRepositoryId}");
                        var branches = await _giteaRepositoryService.GetRepositoryBranchesAsync(mapping.GiteaRepositoryId);
                        if (branches != null && branches.Any())
                        {
                            ViewBag.Branches = branches;
                            _logger.LogInformation($"Found {branches.Count} branches for repository {id}");
                            
                            // Nếu không có branch được chỉ định, sử dụng branch đầu tiên
                            if (string.IsNullOrEmpty(branch))
                            {
                                ViewBag.CurrentBranch = branches.First().Name;
                                _logger.LogInformation($"Using first branch: {ViewBag.CurrentBranch}");
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"No branches found for repository {id}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error fetching branches for repository {id} from Gitea");
                        // Don't rethrow - just log and continue
                    }

                    // Nếu có branch và kết nối đến Gitea thành công, thử lấy nội dung từ Gitea
                    if (!string.IsNullOrEmpty(ViewBag.CurrentBranch))
                    {
                        try
                        {
                            _logger.LogInformation($"Fetching directory contents for repository {id}, branch {ViewBag.CurrentBranch}, path {path ?? "root"}");
                            
                            var directoryContents = await _giteaRepositoryService.GetDirectoryContentsAsync(
                                mapping.GiteaRepositoryId, 
                                ViewBag.CurrentBranch, 
                                path
                            );
                            
                            if (directoryContents != null)
                            {
                                ViewBag.DirectoryContents = directoryContents;
                                _logger.LogInformation($"Successfully loaded {directoryContents.Count} items from Gitea for repository {id}");
                            }
                            else
                            {
                                _logger.LogWarning($"No content returned from Gitea for repository {id}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error fetching directory contents for repository {id} from Gitea");
                            TempData["WarningMessage"] = "Failed to load content from Gitea. Please try again later.";
                            // Don't rethrow - just log and continue
                        }
                    }
                }

                return View(repository);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Details action for repository ID {id}");
                TempData["ErrorMessage"] = "An error occurred while loading the repository details. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Repository/DetailsByName?owner=xxx&repo=yyy
        [HttpGet("Repository/DetailsByName")]
        public async Task<IActionResult> DetailsByName(string owner, string repo, string branch = null, string path = null)
        {
            try
            {
                _logger.LogInformation($"Accessing DetailsByName with owner={owner}, repo={repo}, branch={branch}, path={path}");
                
                if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
                {
                    _logger.LogWarning("Owner or repository name is empty");
                    TempData["ErrorMessage"] = "Owner and repository name are required.";
                    return RedirectToAction(nameof(Index));
                }
                
                // Tìm repository dựa trên owner và repo name
                _logger.LogInformation($"Searching for repository {owner}/{repo} in local database");
                var repository = _repositoryService.GetRepositoryByName(owner, repo);
                
                if (repository != null)
                {
                    _logger.LogInformation($"Repository found in DB with ID {repository.RepositoryId}, redirecting to Details");
                    return RedirectToAction(nameof(Details), new { 
                        id = repository.RepositoryId, 
                        branch = branch, 
                        path = path 
                    });
                }
                else
                {
                    _logger.LogInformation($"Repository {owner}/{repo} not found in local database");
                    
                    // Nếu máy chủ Gitea không hoạt động, thông báo cho người dùng
                    var giteaConnectivity = await TestGiteaConnectivityAsync();
                    if (!giteaConnectivity)
                    {
                        _logger.LogError("Gitea server is not accessible");
                        TempData["ErrorMessage"] = "Cannot connect to the Gitea server. Please check if Gitea is running and try again later.";
                        return RedirectToAction(nameof(Index));
                    }
                }
                
                // Nếu repository không tồn tại trong DB nhưng người dùng đã đăng nhập
                // Thử lấy dữ liệu từ Gitea
                if (User.Identity.IsAuthenticated)
                {
                    try
                    {
                        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                        {
                            _logger.LogWarning("Invalid user identifier format");
                            TempData["ErrorMessage"] = "Authentication error: invalid user format.";
                            return RedirectToAction(nameof(Index));
                        }
                        
                        _logger.LogInformation($"User authenticated with ID={userId}, checking Gitea repositories");
                        
                        // Lấy danh sách repositories của user từ Gitea
                        var giteaRepos = await _giteaRepositoryService.GetUserRepositoriesAsync(userId);
                        
                        if (giteaRepos == null || !giteaRepos.Any())
                        {
                            _logger.LogWarning($"No repositories found in Gitea for user {userId}");
                            TempData["ErrorMessage"] = "No repositories found in your Gitea account.";
                            return RedirectToAction(nameof(Index));
                        }
                        
                        _logger.LogInformation($"Found {giteaRepos.Count} repositories in Gitea for user {userId}");
                        
                        // Tìm repository cần thiết
                        var giteaRepo = giteaRepos.FirstOrDefault(r => 
                            r.Owner != null && 
                            r.Owner.Login != null && 
                            r.Owner.Login.Equals(owner, StringComparison.OrdinalIgnoreCase) && 
                            r.Name != null && 
                            r.Name.Equals(repo, StringComparison.OrdinalIgnoreCase));
                        
                        if (giteaRepo != null)
                        {
                            _logger.LogInformation($"Found repository {owner}/{repo} in Gitea, creating local mapping");
                            // Tạo repository mới trong hệ thống
                            var newRepo = new Repository
                            {
                                OwnerId = userId,
                                RepositoryName = giteaRepo.Name,
                                Description = giteaRepo.Description,
                                Visibility = giteaRepo.Private ? "Private" : "Public",
                                DefaultBranch = giteaRepo.DefaultBranch,
                                CreatedDate = giteaRepo.CreatedAt,
                                UpdatedDate = giteaRepo.UpdatedAt
                            };
                            
                            _repositoryService.CreateRepository(newRepo);
                            
                            // Tạo mapping cho repository
                            _repositoryMappingService.CreateMapping(
                                newRepo.RepositoryId,
                                giteaRepo.Id,
                                giteaRepo.HtmlUrl,
                                giteaRepo.CloneUrl,
                                giteaRepo.SshUrl
                            );
                            
                            _logger.LogInformation($"Created new repository mapping for {owner}/{repo} with ID {newRepo.RepositoryId}");
                            
                            // Redirect đến trang xem repository theo ID
                            return RedirectToAction(nameof(Details), new { 
                                id = newRepo.RepositoryId, 
                                branch = branch, 
                                path = path 
                            });
                        }
                        
                        // Không tìm thấy repository trong Gitea
                        TempData["ErrorMessage"] = $"Repository '{repo}' not found for user '{owner}'.";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing repository {owner}/{repo} from Gitea");
                        TempData["ErrorMessage"] = "Error loading repository data from Gitea: " + ex.Message;
                        return RedirectToAction(nameof(Index));
                    }
                }
                
                // Người dùng chưa đăng nhập
                TempData["ErrorMessage"] = $"Repository '{owner}/{repo}' not found. You may need to log in to view this repository.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading repository details for {owner}/{repo}");
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
                
                // Get repositories from our database
                var devCommunityRepos = _repositoryService.GetRepositoriesByOwner(userId);
                
                // Get repositories from Gitea
                var giteaRepos = await _giteaRepositoryService.GetUserRepositoriesAsync(userId);
                
                // Check if there are repositories in Gitea that don't exist in DevCommunity
                // This could happen if a user created repos directly on Gitea
                await SynchronizeGiteaRepositoriesAsync(userId, devCommunityRepos, giteaRepos);
                
                // Refresh DevCommunity repositories after synchronization
                devCommunityRepos = _repositoryService.GetRepositoriesByOwner(userId);
                
                var viewModel = new MyRepositoriesViewModel
                {
                    DevCommunityRepositories = devCommunityRepos,
                    GiteaRepositories = giteaRepos
                };
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user repositories");
                TempData["ErrorMessage"] = "Error loading your repositories. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }
        
        /// <summary>
        /// Synchronize repositories from Gitea to DevCommunity
        /// </summary>
        private async Task SynchronizeGiteaRepositoriesAsync(int userId, IEnumerable<Repository> devCommunityRepos, List<RepositoryResponse> giteaRepos)
        {
            try
            {
                // Get the user to check if they have a linked Gitea account
                var user = _userService.GetUserById(userId);
                if (user == null || string.IsNullOrEmpty(user.GiteaUsername) || string.IsNullOrEmpty(user.GiteaToken))
                {
                    // User doesn't have a linked Gitea account, no synchronization needed
                    return;
                }
                
                // Build a dictionary of existing repo names in DevCommunity for quick lookup
                var existingRepoNames = devCommunityRepos
                    .Select(r => r.RepositoryName.ToLowerInvariant())
                    .ToHashSet();
                
                // Find Gitea repos that don't exist in DevCommunity
                var newRepos = giteaRepos
                    .Where(gr => !existingRepoNames.Contains(gr.Name.ToLowerInvariant()))
                    .ToList();
                
                // Add new repositories to DevCommunity
                foreach (var giteaRepo in newRepos)
                {
                    // Create a new repository entry in DevCommunity
                    var newRepo = new Repository
                    {
                        OwnerId = userId,
                        RepositoryName = giteaRepo.Name,
                        Description = giteaRepo.Description,
                        Visibility = giteaRepo.Private ? "Private" : "Public",
                        DefaultBranch = giteaRepo.DefaultBranch,
                        CreatedDate = giteaRepo.CreatedAt,
                        UpdatedDate = giteaRepo.UpdatedAt
                    };
                    
                    _repositoryService.CreateRepository(newRepo);
                    _logger.LogInformation($"Synchronized Gitea repository '{giteaRepo.Name}' to DevCommunity for user {userId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error synchronizing Gitea repositories for user {userId}");
                // We don't throw the exception to avoid interrupting the main flow
            }
        }

        // GET: /Repository/FileContent
        [HttpGet]
        public async Task<IActionResult> FileContent(string owner, string repo, string path, string branch = "main")
        {
            try
            {
                if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo) || string.IsNullOrEmpty(path))
                {
                    return BadRequest("Owner, repository name, and file path are required");
                }

                if (!User.Identity.IsAuthenticated)
                {
                    TempData["ErrorMessage"] = "You need to be logged in to view file content";
                    return RedirectToAction("Login", "Account");
                }

                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                
                // Lấy repository từ Gitea
                try
                {
                    var fileContent = await _giteaRepositoryService.GetFileContentAsync(userId, owner, repo, path, branch);
                    
                    if (fileContent == null)
                    {
                        TempData["ErrorMessage"] = "Failed to get file content";
                        return RedirectToAction("Details", new { owner, repo, branch });
                    }
                    
                    // Tạo view model
                    var viewModel = new FileContentViewModel
                    {
                        FileName = System.IO.Path.GetFileName(path),
                        FilePath = path,
                        Content = fileContent.Content,
                        Encoding = fileContent.Encoding,
                        Owner = owner,
                        Repository = repo,
                        Branch = branch,
                        Size = fileContent.Size,
                        HtmlUrl = fileContent.HtmlUrl
                    };
                    
                    return View(viewModel);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting file content");
                    TempData["ErrorMessage"] = "Error getting file content: " + ex.Message;
                    return RedirectToAction("Details", new { owner, repo, branch });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error viewing file content");
                TempData["ErrorMessage"] = "Error viewing file content";
                return RedirectToAction("Index");
            }
        }

        // GET: /Repository/ViewGiteaRepository
        [Authorize]
        public async Task<IActionResult> ViewGiteaRepository(string owner, string repo)
        {
            try
            {
                if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
                {
                    return BadRequest("Owner and repository name are required.");
                }
                
                // Get current user ID
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }
                
                // Get Gitea login URL from the service
                var giteaLoginUrl = await _giteaUserSyncService.GetGiteaLoginUrlAsync(userId);
                
                if (string.IsNullOrEmpty(giteaLoginUrl))
                {
                    // If there's no linked account, redirect to the link page
                    TempData["ErrorMessage"] = "You need to link your Gitea account first.";
                    return RedirectToAction("LinkGiteaAccount", "Account");
                }
                
                // If it's already a full URL, use it directly
                if (giteaLoginUrl.StartsWith("http"))
                {
                    return Redirect($"{giteaLoginUrl}/{owner}/{repo}");
                }
                
                // Otherwise, it's a session ID, so construct the URL
                return Redirect($"http://localhost:3000/{owner}/{repo}?_session={giteaLoginUrl}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accessing Gitea repository");
                TempData["ErrorMessage"] = "Error accessing Gitea repository.";
                return RedirectToAction(nameof(MyRepositories));
            }
        }
        
        // GET: /Repository/Branches
        [Authorize]
        public async Task<IActionResult> Branches(string owner, string repo)
        {
            try
            {
                if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
                {
                    return BadRequest("Owner and repository name are required.");
                }
                
                // Get current user ID
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }
                
                // Get repository details
                var repository = _repositoryService.GetRepositoryByName(owner, repo);
                if (repository == null)
                {
                    TempData["ErrorMessage"] = "Repository not found in DevCommunity database.";
                    return RedirectToAction(nameof(Index));
                }
                
                // Get branches from Gitea
                var branches = await _giteaRepositoryService.GetBranchesAsync(userId, owner, repo);
                
                // Create the view model
                var viewModel = new BranchListViewModel
                {
                    Owner = owner,
                    RepositoryName = repo,
                    Repository = repository,
                    Branches = branches,
                    DefaultBranch = repository.DefaultBranch,
                    IsOwner = repository.OwnerId == userId,
                    CurrentBranch = repository.DefaultBranch
                };
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading branches for repository {owner}/{repo}");
                TempData["ErrorMessage"] = "Error loading repository branches. Please try again later.";
                return RedirectToAction(nameof(Details), new { owner, repo });
            }
        }
        
        // GET: /Repository/CommitHistory
        [Authorize]
        public async Task<IActionResult> CommitHistory(string owner, string repo, string branch = "main", string path = "")
        {
            try
            {
                if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
                {
                    return BadRequest("Owner and repository name are required.");
                }
                
                // Get current user ID
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }
                
                // Get repository details
                var repository = _repositoryService.GetRepositoryByName(owner, repo);
                if (repository == null)
                {
                    TempData["ErrorMessage"] = "Repository not found in DevCommunity database.";
                    return RedirectToAction(nameof(Index));
                }
                
                // Get commits from Gitea
                var commits = await _giteaRepositoryService.GetCommitHistoryAsync(userId, owner, repo, branch, path);
                
                // Create the view model
                var viewModel = new CommitHistoryViewModel
                {
                    Owner = owner,
                    RepositoryName = repo,
                    Repository = repository,
                    Branch = branch,
                    Commits = commits,
                    IsOwner = repository.OwnerId == userId,
                    Path = path
                };
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading commit history for repository {owner}/{repo}");
                TempData["ErrorMessage"] = "Error loading commit history. Please try again later.";
                return RedirectToAction(nameof(Details), new { owner, repo });
            }
        }
        
        // GET: /Repository/CreateBranch
        [Authorize]
        public async Task<IActionResult> CreateBranch(string owner, string repo, string sourceBranch = "main")
        {
            try
            {
                if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
                {
                    return BadRequest("Owner and repository name are required.");
                }
                
                // Get current user ID
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }
                
                // Get repository details
                var repository = _repositoryService.GetRepositoryByName(owner, repo);
                if (repository == null)
                {
                    TempData["ErrorMessage"] = "Repository not found in DevCommunity database.";
                    return RedirectToAction(nameof(Index));
                }
                
                // Check if user is the owner
                if (repository.OwnerId != userId)
                {
                    TempData["ErrorMessage"] = "You don't have permission to create branches in this repository.";
                    return RedirectToAction(nameof(Details), new { owner, repo });
                }
                
                // Get branches from Gitea
                var branches = await _giteaRepositoryService.GetBranchesAsync(userId, owner, repo);
                
                // Create the view model
                var viewModel = new CreateBranchViewModel
                {
                    Owner = owner,
                    RepositoryName = repo,
                    Repository = repository,
                    SourceBranch = sourceBranch,
                    AvailableBranches = branches
                };
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error preparing branch creation form for repository {owner}/{repo}");
                TempData["ErrorMessage"] = "Error preparing branch creation form. Please try again later.";
                return RedirectToAction(nameof(Branches), new { owner, repo });
            }
        }
        
        // POST: /Repository/CreateBranch
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBranch(CreateBranchViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Get current user ID
                    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                    if (userId == 0)
                    {
                        return RedirectToAction("Login", "Account");
                    }
                    
                    // Get repository details
                    var repository = _repositoryService.GetRepositoryByName(model.Owner, model.RepositoryName);
                    if (repository == null)
                    {
                        TempData["ErrorMessage"] = "Repository not found in DevCommunity database.";
                        return RedirectToAction(nameof(Index));
                    }
                    
                    // Check if user is the owner
                    if (repository.OwnerId != userId)
                    {
                        TempData["ErrorMessage"] = "You don't have permission to create branches in this repository.";
                        return RedirectToAction(nameof(Details), new { owner = model.Owner, repo = model.RepositoryName });
                    }
                    
                    // Create the branch in Gitea
                    var result = await _giteaRepositoryService.CreateBranchAsync(
                        userId, 
                        model.Owner, 
                        model.RepositoryName, 
                        model.NewBranchName, 
                        model.SourceBranch);
                    
                    if (result)
                    {
                        TempData["SuccessMessage"] = $"Branch '{model.NewBranchName}' created successfully.";
                        return RedirectToAction(nameof(Branches), new { owner = model.Owner, repo = model.RepositoryName });
                    }
                    else
                    {
                        ModelState.AddModelError("", "Failed to create branch. It may already exist or there might be an issue with the Gitea server.");
                    }
                }
                
                // If we get here, something went wrong - reload the form with branches
                var branches = await _giteaRepositoryService.GetBranchesAsync(
                    int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)), 
                    model.Owner, 
                    model.RepositoryName);
                
                model.AvailableBranches = branches;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating branch for repository {model.Owner}/{model.RepositoryName}");
                TempData["ErrorMessage"] = "Error creating branch. Please try again later.";
                return RedirectToAction(nameof(Branches), new { owner = model.Owner, repo = model.RepositoryName });
            }
        }

        // GET: /Repository/SyncRepository/5
        [HttpGet]
        public async Task<IActionResult> SyncRepository(int id)
        {
            try
            {
                // Lấy thông tin repository từ DB
                var repository = _repositoryService.GetRepositoryWithOwner(id);
                
                if (repository == null)
                {
                    TempData["ErrorMessage"] = "Repository not found.";
                    return RedirectToAction(nameof(Index));
                }
                
                // Lấy mapping giữa DevCommunity và Gitea
                var mapping = _repositoryMappingService.GetByDevCommunityId(repository.RepositoryId);
                if (mapping == null)
                {
                    TempData["ErrorMessage"] = "This repository is not linked to a Gitea repository.";
                    return RedirectToAction(nameof(Details), new { id });
                }
                
                // Kiểm tra người dùng đã đăng nhập
                if (!User.Identity.IsAuthenticated)
                {
                    TempData["ErrorMessage"] = "You need to be logged in to sync repositories.";
                    return RedirectToAction("Login", "Account");
                }
                
                if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                {
                    TempData["ErrorMessage"] = "Invalid user authentication information.";
                    return RedirectToAction(nameof(Details), new { id });
                }
                
                // Kiểm tra OwnerId có giá trị hay không
                if (!repository.OwnerId.HasValue)
                {
                    TempData["ErrorMessage"] = "This repository doesn't have an owner assigned.";
                    return RedirectToAction(nameof(Details), new { id });
                }
                
                // Lấy thông tin chủ sở hữu từ database
                var owner = _userService.GetUserById(repository.OwnerId.Value);
                if (owner == null)
                {
                    TempData["ErrorMessage"] = "Repository owner information not found.";
                    return RedirectToAction(nameof(Details), new { id });
                }
                
                if (string.IsNullOrEmpty(owner.GiteaUsername))
                {
                    TempData["ErrorMessage"] = "Repository owner doesn't have a Gitea account.";
                    return RedirectToAction(nameof(Details), new { id });
                }
                
                // Kiểm tra quyền truy cập Gitea
                var userResult = await _giteaUserSyncService.EnsureGiteaUserWithDetailsAsync(userId);
                if (!userResult.Success)
                {
                    TempData["ErrorMessage"] = $"Failed to authenticate with Gitea: {userResult.ErrorMessage}";
                    return RedirectToAction(nameof(Details), new { id });
                }
                
                // Cập nhật mapping từ Gitea
                try
                {
                    // Lấy thông tin repository từ Gitea
                    var giteaRepos = await _giteaRepositoryService.GetUserRepositoriesAsync(userId);
                    
                    if (giteaRepos == null || !giteaRepos.Any())
                    {
                        TempData["ErrorMessage"] = "Could not retrieve repositories from Gitea. The server may be unavailable.";
                        return RedirectToAction(nameof(Details), new { id });
                    }
                    
                    var giteaRepo = giteaRepos.FirstOrDefault(r => 
                        r.Owner != null && 
                        r.Owner.Login != null && 
                        r.Owner.Login.Equals(owner.GiteaUsername, StringComparison.OrdinalIgnoreCase) && 
                        r.Name != null && 
                        r.Name.Equals(repository.RepositoryName, StringComparison.OrdinalIgnoreCase));
                    
                    if (giteaRepo != null)
                    {
                        // Cập nhật thông tin mapping
                        mapping.GiteaRepositoryId = giteaRepo.Id;
                        mapping.CloneUrl = giteaRepo.CloneUrl;
                        mapping.LastSyncDate = DateTime.UtcNow;
                        _repositoryMappingService.UpdateMapping(mapping);
                        
                        // Cập nhật thông tin repository
                        repository.DefaultBranch = giteaRepo.DefaultBranch;
                        repository.UpdatedDate = giteaRepo.UpdatedAt;
                        repository.Description = giteaRepo.Description;
                        _repositoryService.UpdateRepository(repository);
                        
                        TempData["SuccessMessage"] = "Repository synchronized successfully.";
                    }
                    else
                    {
                        TempData["WarningMessage"] = $"Repository '{repository.RepositoryName}' not found in Gitea for user '{owner.GiteaUsername}'. It may have been deleted or renamed.";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error synchronizing repository with Gitea: {Message}", ex.Message);
                    TempData["ErrorMessage"] = "Error synchronizing repository with Gitea: " + ex.Message;
                }
                
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error synchronizing repository: {Message}", ex.Message);
                TempData["ErrorMessage"] = "An error occurred while synchronizing the repository.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Repository/TestGiteaConnection
        [HttpGet]
        [Route("Repository/TestGiteaConnection")]
        public async Task<IActionResult> TestGiteaConnection()
        {
            try
            {
                _logger.LogInformation("Testing Gitea server connection");
                
                // Kiểm tra kết nối với Gitea server
                var isConnected = await TestGiteaConnectivityAsync();
                
                if (isConnected)
                {
                    _logger.LogInformation("Gitea server connection test successful");
                    TempData["SuccessMessage"] = "Gitea server connection successful";
                }
                else
                {
                    _logger.LogError("Gitea server connection test failed");
                    TempData["ErrorMessage"] = "Gitea server connection failed";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing Gitea server connection");
                TempData["ErrorMessage"] = $"Error testing Gitea server connection: {ex.Message}";
            }
            
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Kiểm tra kết nối với máy chủ Gitea
        /// </summary>
        private async Task<bool> TestGiteaConnectivityAsync()
        {
            try
            {
                _logger.LogInformation("Starting Gitea connectivity test");
                
                var giteaService = HttpContext.RequestServices.GetService<IGiteaIntegrationService>();
                if (giteaService == null)
                {
                    _logger.LogError("IGiteaIntegrationService not available in service container");
                    return false;
                }
                
                _logger.LogInformation("IGiteaIntegrationService obtained, testing server connectivity");
                
                var result = await giteaService.TestGiteaServerConnectivityAsync();
                
                _logger.LogInformation($"Gitea connectivity test result: {result}");
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing Gitea connectivity: {Message}", ex.Message);
                return false;
            }
        }
    }
} 