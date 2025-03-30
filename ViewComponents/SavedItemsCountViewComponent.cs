using Microsoft.AspNetCore.Mvc;
using DoAnWeb.Models;
using DoAnWeb.Repositories;
using System.Security.Claims;

namespace DoAnWeb.ViewComponents
{
    public class SavedItemsCountViewComponent : ViewComponent
    {
        private readonly IUserSavedItemRepository _savedItemRepository;

        public SavedItemsCountViewComponent(IUserSavedItemRepository savedItemRepository)
        {
            _savedItemRepository = savedItemRepository;
        }

        public IViewComponentResult Invoke()
        {
            // Default to 0 if not authenticated
            int savedItemsCount = 0;

            // Check if user is authenticated
            if (UserClaimsPrincipal?.Identity != null && UserClaimsPrincipal.Identity.IsAuthenticated)
            {
                // Get the user ID from claims
                var userIdClaim = UserClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    // Get saved items from repository
                    var savedItems = _savedItemRepository.GetSavedItemsByUserId(userId);
                    savedItemsCount = savedItems.Count();
                }
            }

            // Return the count directly
            return View(savedItemsCount);
        }
    }
} 