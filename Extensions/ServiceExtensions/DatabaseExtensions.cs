using DoAnWeb.Models;
using DoAnWeb.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DoAnWeb.Extensions.ServiceExtensions
{
    public static class DatabaseExtensions
    {
        /// <summary>
        /// Adds database context and configures connection settings
        /// </summary>
        public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Read connection string from appsettings.json
            var connectionString = configuration.GetConnectionString("DevCommunityDB");

            // Configure Entity Framework Core with SQL Server and performance optimizations
            services.AddDbContext<DevCommunityContext>(options =>
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(3); // Automatically retry connection on failure (up to 3 times)
                    sqlOptions.CommandTimeout(30);      // Increase timeout for complex queries (30 seconds)
                }));

            return services;
        }

        /// <summary>
        /// Registers all repository services
        /// </summary>
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            // Register generic and specific repositories using dependency injection
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IQuestionRepository, QuestionRepository>();
            services.AddScoped<IRepositoryRepository, RepositoryRepository>();
            services.AddScoped<IRepositoryMappingRepository, RepositoryMappingRepository>();
            services.AddScoped<IUserSavedItemRepository, UserSavedItemRepository>();
            services.AddScoped<IRepository<Vote>, Repository<Vote>>();
            services.AddScoped<IRepository<Answer>, Repository<Answer>>();
            services.AddScoped<IRepository<Tag>, Repository<Tag>>();
            services.AddScoped<ICommentRepository, CommentRepository>();

            return services;
        }
    }
} 