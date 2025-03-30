using DoAnWeb.Middleware;

namespace DoAnWeb.Extensions
{
    public static class MiddlewareExtensions
    {
        /// <summary>
        /// Adds the SavedItemsMiddleware to the application pipeline
        /// </summary>
        public static IApplicationBuilder UseSavedItemsMiddleware(this IApplicationBuilder app)
        {
            return app.UseMiddleware<SavedItemsMiddleware>();
        }
    }
} 