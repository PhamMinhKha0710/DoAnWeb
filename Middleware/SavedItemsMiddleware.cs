using System.Security.Claims;
using DoAnWeb.Models;
using DoAnWeb.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DoAnWeb.Middleware
{
    public class SavedItemsMiddleware
    {
        private readonly RequestDelegate _next;

        public SavedItemsMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, DevCommunityContext dbContext, IUserSavedItemRepository savedItemRepository)
        {
            // Only proceed for authenticated users
            if (context.User.Identity.IsAuthenticated)
            {
                // Get the current user ID
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    // Ensure user exists in database
                    var user = await dbContext.Users.FindAsync(userId);
                    if (user != null)
                    {
                        // Check if user has any saved items
                        var savedItems = savedItemRepository.GetSavedItemsByUserId(userId);
                        int savedItemsCount = savedItems.Count();

                        // Set data in ViewData for the view to access
                        if (context.Items.ContainsKey("ViewBag"))
                        {
                            var viewBag = context.Items["ViewBag"];
                            // We don't actually need to set this here anymore since we're using a ViewComponent
                            // But we'll keep the code as a reference for other similar middleware
                        }
                    }
                }
            }

            // Call the next delegate/middleware in the pipeline
            await _next(context);
        }
    }

    // Extension method for adding the middleware to the HTTP request pipeline
    public static class SavedItemsMiddlewareExtensions
    {
        public static IApplicationBuilder UseSavedItemsMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SavedItemsMiddleware>();
        }
    }
} 