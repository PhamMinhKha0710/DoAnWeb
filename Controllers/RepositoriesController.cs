using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DoAnWeb.Models;
using DoAnWeb.Services;
using DoAnWeb.ViewModels;
using System.Security.Claims;

namespace DoAnWeb.Controllers
{
    public class RepositoriesController : Controller
    {
        private readonly IRepositoryService _repositoryService;
        private readonly IUserService _userService;

        public RepositoriesController(IRepositoryService repositoryService, IUserService userService)
        {
            _repositoryService = repositoryService;
            _userService = userService;
        }

        // GET: Repositories
        public IActionResult Index(string searchTerm = null)
        {
            IEnumerable<Repository> repositories;

            if (!string.IsNullOrEmpty(searchTerm))
            {
                repositories = _repositoryService.SearchRepositories(searchTerm);
                ViewData["SearchTerm"] = searchTerm;
            }
            else
            {
                repositories = _repositoryService.GetAllRepositories();
            }

            return View(repositories);
        }

        // GET: Repositories/Details/5
        public IActionResult Details(int id)
        {
            var repository = _repositoryService.GetRepositoryWithOwner(id);
            if (repository == null)
            {
                return NotFound();
            }

            return View(repository);
        }

        // GET: Repositories/Files/5
        public IActionResult Files(int id)
        {
            var repository = _repositoryService.GetRepositoryWithFiles(id);
            if (repository == null)
            {
                return NotFound();
            }

            return View(repository);
        }

        // GET: Repositories/Commits/5
        public IActionResult Commits(int id)
        {
            var repository = _repositoryService.GetRepositoryWithCommits(id);
            if (repository == null)
            {
                return NotFound();
            }

            return View(repository);
        }

        // GET: Repositories/Create
        [Authorize]
        public IActionResult Create()
        {
            return View(new RepositoryViewModel());
        }

        // POST: Repositories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Create(RepositoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Get current user ID from claims
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                    if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                        return RedirectToAction("Login", "Account");

                    // Create repository
                    var repository = new Repository
                    {
                        RepositoryName = model.RepositoryName,
                        Description = model.Description,
                        Visibility = model.Visibility,
                        OwnerId = userId
                    };

                    _repositoryService.CreateRepository(repository);

                    return RedirectToAction(nameof(Details), new { id = repository.RepositoryId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            return View(model);
        }

        // GET: Repositories/Edit/5
        [Authorize]
        public IActionResult Edit(int id)
        {
            var repository = _repositoryService.GetRepositoryById(id);
            if (repository == null)
            {
                return NotFound();
            }

            // Check if user is the owner of the repository
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId) || repository.OwnerId != userId)
            {
                return Forbid();
            }

            // Create view model
            var model = new RepositoryViewModel
            {
                RepositoryId = repository.RepositoryId,
                RepositoryName = repository.RepositoryName,
                Description = repository.Description,
                Visibility = repository.Visibility
            };

            return View(model);
        }

        // POST: Repositories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Edit(int id, RepositoryViewModel model)
        {
            if (id != model.RepositoryId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get repository
                    var repository = _repositoryService.GetRepositoryById(id);
                    if (repository == null)
                    {
                        return NotFound();
                    }

                    // Check if user is the owner of the repository
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                    if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId) || repository.OwnerId != userId)
                    {
                        return Forbid();
                    }

                    // Update repository
                    repository.RepositoryName = model.RepositoryName;
                    repository.Description = model.Description;
                    repository.Visibility = model.Visibility;

                    _repositoryService.UpdateRepository(repository);

                    return RedirectToAction(nameof(Details), new { id = repository.RepositoryId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            return View(model);
        }

        // GET: Repositories/Delete/5
        [Authorize]
        public IActionResult Delete(int id)
        {
            var repository = _repositoryService.GetRepositoryWithOwner(id);
            if (repository == null)
            {
                return NotFound();
            }

            // Check if user is the owner of the repository
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId) || repository.OwnerId != userId)
            {
                return Forbid();
            }

            return View(repository);
        }

        // POST: Repositories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult DeleteConfirmed(int id)
        {
            var repository = _repositoryService.GetRepositoryById(id);
            if (repository == null)
            {
                return NotFound();
            }

            // Check if user is the owner of the repository
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId) || repository.OwnerId != userId)
            {
                return Forbid();
            }

            _repositoryService.DeleteRepository(id);
            return RedirectToAction(nameof(Index));
        }

        // GET: Repositories/CreateFile/5
        [Authorize]
        public IActionResult CreateFile(int id)
        {
            var repository = _repositoryService.GetRepositoryById(id);
            if (repository == null)
            {
                return NotFound();
            }

            // Check if user is the owner of the repository
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId) || repository.OwnerId != userId)
            {
                return Forbid();
            }

            var model = new RepositoryFileViewModel
            {
                RepositoryId = id
            };

            return View(model);
        }

        // POST: Repositories/CreateFile/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult CreateFile(RepositoryFileViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Get repository
                    var repository = _repositoryService.GetRepositoryById(model.RepositoryId);
                    if (repository == null)
                    {
                        return NotFound();
                    }

                    // Check if user is the owner of the repository
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                    if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId) || repository.OwnerId != userId)
                    {
                        return Forbid();
                    }

                    // Create file
                    var file = new RepositoryFile
                    {
                        RepositoryId = model.RepositoryId,
                        FilePath = model.FilePath,
                        FileContent = model.FileContent,
                        FileHash = ComputeHash(model.FileContent)
                    };

                    _repositoryService.AddFile(file);

                    // Create commit
                    var commit = new RepositoryCommit
                    {
                        RepositoryId = model.RepositoryId,
                        AuthorId = userId,
                        CommitMessage = $"Added file: {model.FilePath}"
                    };

                    _repositoryService.CreateCommit(commit);

                    return RedirectToAction(nameof(Files), new { id = model.RepositoryId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            return View(model);
        }

        // Helper method to compute file hash
        private string ComputeHash(string content)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }
    }
}