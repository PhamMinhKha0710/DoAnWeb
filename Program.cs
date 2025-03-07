using DoAnWeb;
using DoAnWeb.Repositories;
using DoAnWeb.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ✅ 1. Đọc chuỗi kết nối từ appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DevCommunityDB");

// ✅ 2. Cấu hình Entity Framework Core với SQL Server
builder.Services.AddDbContext<DevCommunityContext>(options =>
    options.UseSqlServer(connectionString));

// ✅ 3. Đăng ký Repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
builder.Services.AddScoped<IRepositoryRepository, RepositoryRepository>();

// ✅ 4. Đăng ký Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<IRepositoryService, RepositoryService>();

// ✅ 5. Thêm dịch vụ MVC (Controllers + Views)
builder.Services.AddControllersWithViews();

// ✅ 6. Thêm Authentication
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.Cookie.Name = "DevCommunityAuth";
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

var app = builder.Build();

// ✅ 4. Middleware xử lý lỗi & bảo mật
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// ✅ 5. Middleware xử lý HTTP & Routing
app.UseHttpsRedirection();
app.UseStaticFiles();  // ⚠️ Sửa từ `MapStaticAssets()` để hỗ trợ file tĩnh

app.UseRouting();

app.UseAuthentication(); // ⚠️ Nếu dùng phân quyền, cần thêm
app.UseAuthorization();

// ✅ 6. Cấu hình định tuyến
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
