using System.Security.Claims;
using DoAnWeb.Models;
using DoAnWeb.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DoAnWeb.Middleware
{
    public class SavedItemsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SavedItemsMiddleware> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public SavedItemsMiddleware(
            RequestDelegate next,
            ILogger<SavedItemsMiddleware> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _next = next;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only proceed for authenticated users
            if (context.User.Identity.IsAuthenticated)
            {
                // Get the current user ID
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    try
                    {
                        // Tạo scope mới để tránh vấn đề đa luồng với DbContext
                        using var scope = _serviceScopeFactory.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<DevCommunityContext>();
                        var savedItemRepository = scope.ServiceProvider.GetRequiredService<IUserSavedItemRepository>();
                        
                        // Ensure user exists in database
                        var user = await dbContext.Users.FindAsync(userId);
                        if (user != null)
                        {
                            // Check if user has any saved items
                            var savedItems = savedItemRepository.GetSavedItemsByUserId(userId);
                            int savedItemsCount = savedItems.Count();

                            // We don't actually need to set this here anymore since we're using a ViewComponent
                            // But we'll keep the code as a reference for other similar middleware
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in SavedItemsMiddleware for user {UserId}", userId);
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