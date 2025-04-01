using DoAnWeb.Filters;
using DoAnWeb.GitIntegration;
using DoAnWeb.Services;

namespace DoAnWeb.Extensions.ServiceExtensions
{
    public static class ApplicationServiceExtensions
    {
        /// <summary>
        /// Registers all business logic services
        /// </summary>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Register business logic services using dependency injection
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IQuestionService, QuestionService>();
            services.AddScoped<IRepositoryService, RepositoryService>();
            services.AddScoped<IRepositoryMappingService, RepositoryMappingService>();
            services.AddScoped<IAnswerService, AnswerService>();
            services.AddScoped<IQuestionRealTimeService, QuestionRealTimeService>();
            services.AddScoped<IMarkdownService, MarkdownService>();
            services.AddScoped<IBadgeService, BadgeService>();

            // Register password hash service
            services.AddScoped<IPasswordHashService, PasswordHashService>();

            // Register notification services
            services.AddSingleton<NotificationBackgroundService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddHostedService(provider => provider.GetRequiredService<NotificationBackgroundService>());
            
            // Register reputation service
            services.AddScoped<ReputationService>();

            return services;
        }

        /// <summary>
        /// Registers Gitea integration services
        /// </summary>
        public static IServiceCollection AddGiteaServices(this IServiceCollection services)
        {
            // Register Gitea integration services
            services.AddHttpClient<IGiteaIntegrationService, GiteaService>();
            services.AddScoped<IGiteaUserSyncService, GiteaUserSyncService>();
            services.AddScoped<IGiteaRepositoryService, GiteaRepositoryService>();

            return services;
        }

        /// <summary>
        /// Registers MVC-related services with filtering and caching
        /// </summary>
        public static IServiceCollection AddMvcWithFilters(this IServiceCollection services)
        {
            // Register filters
            services.AddScoped<UserInfoFilter>();

            // Add MVC with response caching for improved performance
            services.AddControllersWithViews(options =>
            {
                options.CacheProfiles.Add("Default", new() { Duration = 60 }); // Default cache of 60 seconds
                options.Filters.Add<UserInfoFilter>(); // Add UserInfoFilter to get user avatars
            });

            // Add HttpContextAccessor for accessing HttpContext in views
            services.AddHttpContextAccessor();

            return services;
        }
    }
} 