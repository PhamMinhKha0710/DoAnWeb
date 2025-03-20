using DoAnWeb;
using DoAnWeb.Hubs;
using DoAnWeb.Models;
using DoAnWeb.Repositories;
using DoAnWeb.Services;
using DoAnWeb.GitIntegration;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using DoAnWeb.Utils;

// Create the web application builder
var builder = WebApplication.CreateBuilder(args);

// ✅ 1. Database Configuration
// Read connection string from appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DevCommunityDB");

// Configure Entity Framework Core with SQL Server and performance optimizations
builder.Services.AddDbContext<DevCommunityContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(3); // Automatically retry connection on failure (up to 3 times)
        sqlOptions.CommandTimeout(30);      // Increase timeout for complex queries (30 seconds)
    }));

// ✅ 2. Repository Registration
// Register generic and specific repositories using dependency injection
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
builder.Services.AddScoped<IRepositoryRepository, RepositoryRepository>();
builder.Services.AddScoped<IUserSavedItemRepository, UserSavedItemRepository>();
builder.Services.AddScoped<IRepository<Vote>, Repository<Vote>>();
builder.Services.AddScoped<IRepository<Answer>, Repository<Answer>>();
builder.Services.AddScoped<IRepository<Tag>, Repository<Tag>>();
// builder.Services.AddScoped<IPostRepository, PostRepository>(); // Tạm thời bỏ đăng ký PostRepository vì không có DbSet tương ứng
builder.Services.AddScoped<ICommentRepository, CommentRepository>();

// ✅ 3. Service Registration
// Register business logic services using dependency injection
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<IRepositoryService, RepositoryService>();
builder.Services.AddScoped<IAnswerService, AnswerService>();
builder.Services.AddScoped<IQuestionRealTimeService, QuestionRealTimeService>();
builder.Services.AddScoped<IMarkdownService, MarkdownService>();

// Register password hash service
builder.Services.AddScoped<IPasswordHashService, PasswordHashService>();

// Register Gitea integration services
builder.Services.AddHttpClient<IGiteaIntegrationService, SimpleGiteaService>();
builder.Services.AddScoped<IGiteaUserSyncService, GiteaUserSyncService>();
builder.Services.AddScoped<IGiteaRepositoryService, GiteaRepositoryService>();

// Register notification services
builder.Services.AddSingleton<NotificationBackgroundService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<NotificationBackgroundService>());

// ✅ 4. MVC Configuration
// Add MVC with response caching for improved performance
builder.Services.AddControllersWithViews(options =>
{
    options.CacheProfiles.Add("Default", new() { Duration = 60 }); // Default cache of 60 seconds
});

// ✅ 5. Response Compression
// Add response compression to reduce bandwidth usage and improve load times
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true; // Enable compression for HTTPS connections
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    
    // Add SignalR endpoints to compression
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});

// Configure compression providers for optimal speed
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest; // Optimize for speed
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest; // Optimize for speed
});

// ✅ 6. Memory Cache
// Add in-memory caching for frequently accessed data
builder.Services.AddMemoryCache();

// Đăng ký IHttpContextAccessor để truy cập HttpContext trong view
builder.Services.AddHttpContextAccessor();

// Thêm dịch vụ Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ✅ 7. Authentication
// Configure cookie-based authentication
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.Cookie.Name = "DevCommunityAuth";
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

// ✅ 8. SignalR
// Add SignalR for real-time features with advanced options
builder.Services.AddSignalR(options => 
{
    options.EnableDetailedErrors = true;
    options.MaximumReceiveMessageSize = 102400; // 100 KB
    options.StreamBufferCapacity = 20;
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
});

// ✅ 9. CORS Configuration
// Add CORS policy to allow connections from other devices
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
        builder
            .WithOrigins("https://example.com", "https://sub.example.com")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

// Build the application
var app = builder.Build();

// ✅ 10. Environment-specific Configuration
// Configure error handling and security headers for production
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error"); // Global error handler
    app.UseHsts(); // HTTP Strict Transport Security
}

// Execute the SQL scripts to create database tables
using (var scope = app.Services.CreateScope())
{
    try
    {
        var services = scope.ServiceProvider;
        var configuration = services.GetRequiredService<IConfiguration>();
        var dbUtils = new DbUtils(configuration);
        
        // First execute the entity tables script
        string entityScriptPath = Path.Combine(app.Environment.ContentRootPath, "entity-tables.sql");
        if (File.Exists(entityScriptPath))
        {
            Console.WriteLine("Executing SQL script to create entity tables...");
            Console.WriteLine($"Script path: {entityScriptPath}");
            dbUtils.ExecuteSqlScript(entityScriptPath);
            Console.WriteLine("Entity tables SQL script execution completed.");
        }
        else
        {
            Console.WriteLine($"Entity tables SQL script not found at path: {entityScriptPath}");
        }
        
        // Then execute the chat tables script
        string chatScriptPath = Path.Combine(app.Environment.ContentRootPath, "chat-tables.sql");
        if (File.Exists(chatScriptPath))
        {
            Console.WriteLine("Executing SQL script to create chat tables...");
            Console.WriteLine($"Script path: {chatScriptPath}");
            dbUtils.ExecuteSqlScript(chatScriptPath);
            Console.WriteLine("Chat tables SQL script execution completed.");
        }
        else
        {
            Console.WriteLine($"Chat tables SQL script not found at path: {chatScriptPath}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error executing SQL scripts: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        // Continue application startup even if script execution fails
    }
}

// ✅ 11. Middleware Pipeline
// Configure the HTTP request pipeline with middleware in the correct order

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

// Sử dụng Session
app.UseSession();

// Enable authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Map controllers and SignalR hubs
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map all SignalR hubs
app.MapHub<NotificationHub>("/notificationHub");
app.MapHub<QuestionHub>("/questionHub");
app.MapHub<ViewCountHub>("/viewCountHub");

// Map new real-time hubs
app.MapHub<PresenceHub>("/presenceHub");
app.MapHub<ChatHub>("/chatHub");
app.MapHub<ActivityHub>("/activityHub");

// Start the application
app.Run();
