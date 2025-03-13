using DoAnWeb;
using DoAnWeb.Models;
using DoAnWeb.Repositories;
using DoAnWeb.Services;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;

var builder = WebApplication.CreateBuilder(args);

// ✅ 1. Đọc chuỗi kết nối từ appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DevCommunityDB");

// ✅ 2. Cấu hình Entity Framework Core với SQL Server - với tối ưu hiệu suất
builder.Services.AddDbContext<DevCommunityContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(3); // Tự động thử lại kết nối khi lỗi
        sqlOptions.CommandTimeout(30);      // Tăng timeout cho các truy vấn phức tạp
    }));

// ✅ 3. Đăng ký Repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
builder.Services.AddScoped<IRepositoryRepository, RepositoryRepository>();
builder.Services.AddScoped<IUserSavedItemRepository, UserSavedItemRepository>();
builder.Services.AddScoped<IRepository<Vote>, Repository<Vote>>();
builder.Services.AddScoped<IRepository<Answer>, Repository<Answer>>();
builder.Services.AddScoped<IRepository<Tag>, Repository<Tag>>();

// ✅ 4. Đăng ký Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<IRepositoryService, RepositoryService>();
builder.Services.AddScoped<IAnswerService, AnswerService>();

// ✅ 5. Thêm dịch vụ MVC (Controllers + Views) với caching
builder.Services.AddControllersWithViews(options =>
{
    options.CacheProfiles.Add("Default", new() { Duration = 60 }); // Cache mặc định 60 giây
});

// Thêm Response Compression để giảm kích thước response
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true; // Bật nén cho HTTPS
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

// Cấu hình các provider nén
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest; // Tối ưu tốc độ
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest; // Tối ưu tốc độ
});

// Thêm Memory Cache
builder.Services.AddMemoryCache();

// ✅ 6. Thêm Authentication
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.Cookie.Name = "DevCommunityAuth";
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });



var app = builder.Build();

// app.Use(async (context, next) =>
// {
//     if (context.Request.Path.ToString().Contains("admin", StringComparison.OrdinalIgnoreCase))
//     {
//         await context.Response.WriteAsync("Hello");
//         return; // Dừng middleware để không xử lý tiếp
//     }
//     await next();
// });

// ✅ 4. Middleware xử lý lỗi & bảo mật
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// ✅ 5. Middleware xử lý HTTP & Routing
app.UseHttpsRedirection();

// Bật nén response
app.UseResponseCompression();

// Cấu hình static files với cache
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Cache static files for 7 days
        const int durationInSeconds = 60 * 60 * 24 * 7;
        ctx.Context.Response.Headers.Append("Cache-Control", $"public,max-age={durationInSeconds}");
    }
});

app.UseRouting();

app.UseAuthentication(); // ⚠️ Nếu dùng phân quyền, cần thêm
app.UseAuthorization();

// ✅ 6. Cấu hình định tuyến
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
