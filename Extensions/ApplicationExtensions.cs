using DoAnWeb.Hubs;
using DoAnWeb.Middleware;

namespace DoAnWeb.Extensions
{
    public static class ApplicationExtensions
    {
        /// <summary>
        /// Configures the application middleware pipeline
        /// </summary>
        public static WebApplication ConfigureMiddleware(this WebApplication app)
        {
            // Configure environment-specific middleware
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error"); // Global error handler
                app.UseHsts(); // HTTP Strict Transport Security
            }

            // Redirect HTTP to HTTPS for security
            app.UseHttpsRedirection();

            // Enable response compression
            app.UseResponseCompression();

            // Serve static files (CSS, JS, images)
            app.UseStaticFiles();

            // Enable routing
            app.UseRouting();

            // Enable CORS (must be between UseRouting and UseAuthorization)
            app.UseCors("CorsPolicy");

            // Use Session
            app.UseSession();

            // Enable saved items for all authenticated users
            app.UseSavedItemsMiddleware();

            // Enable authentication and authorization
            app.UseAuthentication();
            app.UseAuthorization();

            return app;
        }

        /// <summary>
        /// Registers all application endpoints
        /// </summary>
        public static WebApplication ConfigureEndpoints(this WebApplication app)
        {
            // Map controllers and SignalR hubs
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // Map API controllers
            app.MapControllerRoute(
                name: "api",
                pattern: "api/{controller}/{action=Index}/{id?}");

            // Map all SignalR hubs
            app.MapHub<NotificationHub>("/notificationHub");
            app.MapHub<QuestionHub>("/questionHub");
            app.MapHub<ViewCountHub>("/viewCountHub");

            // Map new real-time hubs
            app.MapHub<PresenceHub>("/presenceHub");
            app.MapHub<ChatHub>("/chatHub");
            app.MapHub<ActivityHub>("/activityHub");
            app.MapHub<BadgeHub>("/badgeHub");

            return app;
        }
    }
} 