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
        // Tất cả các action sẽ chuyển hướng đến RepositoryController tương ứng
        
        // GET: Repositories
        public IActionResult Index(string searchTerm = null)
        {
            return RedirectToAction("Index", "Repository", new { search = searchTerm });
        }

        // GET: Repositories/Details/5
        public IActionResult Details(int id)
        {
            return RedirectToAction("Details", "Repository", new { id });
        }

        // GET: Repositories/Files/5
        public IActionResult Files(int id)
        {
            return RedirectToAction("Files", "Repository", new { id });
        }

        // GET: Repositories/Commits/5
        public IActionResult Commits(int id)
        {
            return RedirectToAction("Commits", "Repository", new { id });
        }

        // GET: Repositories/Create
        [Authorize]
        public IActionResult Create()
        {
            return RedirectToAction("Create", "Repository");
        }

        // POST: Repositories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Create(RepositoryViewModel model)
        {
            // Chuyển hướng POST không giữ được dữ liệu model, nhưng cũng có thể đến trang Create trống
            return RedirectToAction("Create", "Repository");
        }

        // GET: Repositories/Edit/5
        [Authorize]
        public IActionResult Edit(int id)
        {
            return RedirectToAction("Edit", "Repository", new { id });
        }

        // POST: Repositories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Edit(int id, RepositoryViewModel model)
        {
            // Chuyển hướng POST không giữ được dữ liệu model
            return RedirectToAction("Edit", "Repository", new { id });
        }

        // GET: Repositories/Delete/5
        [Authorize]
        public IActionResult Delete(int id)
        {
            return RedirectToAction("Delete", "Repository", new { id });
        }

        // POST: Repositories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult DeleteConfirmed(int id)
        {
            return RedirectToAction("Delete", "Repository", new { id });
        }

        // GET: Repositories/CreateFile/5
        [Authorize]
        public IActionResult CreateFile(int id)
        {
            return RedirectToAction("CreateFile", "Repository", new { id });
        }

        // POST: Repositories/CreateFile
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult CreateFile(RepositoryFileViewModel model)
        {
            // Chuyển hướng POST không giữ được dữ liệu model
            return RedirectToAction("CreateFile", "Repository");
        }
    }
}