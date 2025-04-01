# 🚀 DevCommunity - Cộng Đồng Lập Trình Viên

DevCommunity là một nền tảng web hiện đại được xây dựng trên ASP.NET Core, kết hợp các tính năng của GitHub và Stack Overflow để tạo một môi trường học tập và chia sẻ kiến thức lập trình hiệu quả.

![DevCommunity Screenshot](https://github.com/user-attachments/assets/4d958968-526b-446a-9379-4e0231cd9ee8)

## 📂 Cấu Trúc Dự Án

### 📁 Mô Hình/Models
Chứa các class đại diện cho cấu trúc dữ liệu:
- `User.cs` - Quản lý thông tin người dùng và xác thực
- `Question.cs`, `Answer.cs`, `Comment.cs` - Mô hình cho hệ thống Q&A
- `Repository.cs`, `RepositoryFile.cs` - Quản lý kho mã nguồn
- `Tag.cs`, `Badge.cs` - Hệ thống tag và huy hiệu thành tựu
- `Vote.cs`, `ReputationHistory.cs` - Hệ thống bình chọn và điểm uy tín
- `DevCommunityContext.cs` - DbContext Entity Framework quản lý kết nối DB

### 🎮 Controllers
Xử lý các request HTTP và điều hướng:
- `AccountController.cs` - Đăng ký, đăng nhập, quản lý tài khoản
- `QuestionsController.cs` - CRUD cho câu hỏi và tìm kiếm
- `RepositoryController.cs` - Quản lý repo mã nguồn
- `VoteController.cs` - Xử lý upvote/downvote
- `BadgeController.cs` - Quản lý và cấp huy hiệu thành tựu
- `TagsController.cs` - Quản lý tag và phân loại nội dung

### 🛠️ Services
Chứa logic nghiệp vụ của ứng dụng:
- `UserService.cs` - Xử lý người dùng và xác thực
- `QuestionService.cs` - Tìm kiếm, sắp xếp và xử lý câu hỏi
- `RepositoryService.cs` - Quản lý mã nguồn và tích hợp Git
- `BadgeService.cs` - Cơ chế cấp huy hiệu thành tựu
- `MarkdownService.cs` - Render Markdown và xử lý định dạng
- `ReputationService.cs` - Tính điểm uy tín và xếp hạng
- `NotificationService.cs` - Quản lý thông báo

### 📡 Hubs
SignalR hubs cho tính năng real-time:
- `ChatHub.cs` - Tin nhắn trực tiếp
- `NotificationHub.cs` - Thông báo thời gian thực
- `QuestionHub.cs` - Cập nhật trạng thái câu hỏi
- `BadgeHub.cs` - Thông báo huy hiệu mới
- `PresenceHub.cs` - Hiển thị trạng thái online
- `ViewCountHub.cs` - Đếm lượt xem thời gian thực

### 🌐 Views
Giao diện người dùng sử dụng Razor:
- `Account/` - Giao diện đăng nhập, đăng ký
- `Questions/` - Hiển thị và tương tác với câu hỏi
- `Repository/` - Quản lý và hiển thị mã nguồn
- `Shared/` - Layout và partial views dùng chung

### ⚙️ GitIntegration
Tích hợp với Gitea để quản lý mã nguồn:
- `GiteaService.cs` - Tương tác với Gitea API
- `GiteaRepositoryService.cs` - Quản lý repo
- `GiteaUserSyncService.cs` - Đồng bộ người dùng với Gitea

### 🧩 Các Thành Phần Khác
- `Middleware/` - Custom middleware cho xử lý request
- `Extensions/` - Extension methods để cấu hình services
- `Repositories/` - Các repository pattern để truy cập dữ liệu
- `ViewComponents/` - Razor view components tái sử dụng
- `ViewModels/` - DTOs và view models
- `Filters/` - Action filters và attribute custom
- `Utils/` - Các tiện ích và helper classes

## ✨ Chức Năng Đã Triển Khai

### 🔐 Hệ Thống Xác Thực
- **Đăng ký và đăng nhập**: Sử dụng ASP.NET Identity với các tùy chỉnh cho DevCommunity
- **OAuth**: Đăng nhập qua Google và GitHub
- **Phân quyền**: Hệ thống Role-based access control (Admin, Moderator, User)

**Cách triển khai**: Sử dụng `UserService` và `AccountController` để xử lý đăng ký, đăng nhập, và quản lý phiên. Mật khẩu được mã hóa bằng `BCrypt` thông qua `PasswordHashService`.

```csharp
// Ví dụ hàm đăng nhập trong AccountController
public async Task<IActionResult> Login(LoginViewModel model)
{
    var user = await _userService.AuthenticateUser(model.Email, model.Password);
    if (user != null)
    {
        await _signInManager.SignInAsync(user, model.RememberMe);
        return RedirectToAction("Index", "Home");
    }
    ModelState.AddModelError("", "Thông tin đăng nhập không chính xác");
    return View(model);
}
```

### ❓ Hệ Thống Q&A
- **Đăng câu hỏi**: Hỗ trợ Markdown, đính kèm tập tin
- **Trả lời và bình luận**: Thảo luận đa cấp
- **Upvote/Downvote**: Đánh giá chất lượng nội dung
- **Tags**: Phân loại câu hỏi theo chủ đề

**Cách triển khai**: `QuestionService` xử lý tìm kiếm và CRUD cho câu hỏi, kết hợp với `MarkdownService` để render nội dung và `VoteController` để quản lý bình chọn.

```csharp
// Ví dụ hàm tìm kiếm câu hỏi trong QuestionService
public async Task<QuestionSearchResult> SearchQuestions(
    string query, 
    int page, 
    string sortBy, 
    List<string> tags,
    int? userId)
{
    var questions = _context.Questions
        .Include(q => q.User)
        .Include(q => q.QuestionTags)
            .ThenInclude(qt => qt.Tag)
        .AsQueryable();
        
    // Lọc theo từ khóa tìm kiếm
    if (!string.IsNullOrEmpty(query))
    {
        questions = questions.Where(q => 
            q.Title.Contains(query) || 
            q.Content.Contains(query));
    }
    
    // Lọc theo tags
    if (tags != null && tags.Count > 0)
    {
        questions = questions.Where(q => 
            q.QuestionTags.Any(t => tags.Contains(t.Tag.Name)));
    }
    
    // Sắp xếp theo tiêu chí
    questions = sortBy switch
    {
        "newest" => questions.OrderByDescending(q => q.CreatedDate),
        "votes" => questions.OrderByDescending(q => q.VoteCount),
        "activity" => questions.OrderByDescending(q => q.LastActivityDate),
        _ => questions.OrderByDescending(q => q.CreatedDate)
    };
    
    // Phân trang
    var totalQuestions = await questions.CountAsync();
    var pageSize = 20;
    var pagedQuestions = await questions
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
        
    return new QuestionSearchResult
    {
        Questions = pagedQuestions,
        TotalCount = totalQuestions,
        CurrentPage = page,
        PageSize = pageSize
    };
}
```

### 📦 Quản Lý Mã Nguồn với Gitea
- **Repository Management**: Tạo và quản lý repo
- **Code Viewing**: Xem mã nguồn với syntax highlighting
- **Integration với Git**: Đồng bộ hóa với Gitea

**Cách triển khai**: `GiteaService` và `RepositoryService` làm việc cùng nhau để đồng bộ dữ liệu giữa ứng dụng và Gitea, cung cấp trải nghiệm Git hoàn chỉnh.

```csharp
// Ví dụ cách lấy danh sách repository từ GiteaService
public async Task<List<RepositoryDto>> GetUserRepositories(string username)
{
    var apiUrl = $"{_giteaOptions.ApiUrl}/users/{username}/repos";
    var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
    request.Headers.Add("Authorization", $"token {_giteaOptions.ApiToken}");
    
    var response = await _httpClient.SendAsync(request);
    if (response.IsSuccessStatusCode)
    {
        var content = await response.Content.ReadAsStringAsync();
        var repositories = JsonConvert.DeserializeObject<List<RepositoryDto>>(content);
        return repositories;
    }
    
    _logger.LogError($"Failed to get repositories for user {username}");
    return new List<RepositoryDto>();
}
```

### 🏆 Hệ Thống Huy Hiệu và Danh Hiệu
- **Achievement Badges**: Huy hiệu thưởng cho các thành tích
- **Reputation Points**: Tính điểm uy tín dựa trên đóng góp
- **User Levels**: Cấp độ người dùng dựa trên hoạt động

**Cách triển khai**: `BadgeService` quản lý việc cấp huy hiệu dựa trên các sự kiện trong hệ thống, kết hợp với `ReputationService` để tính điểm uy tín.

```csharp
// Ví dụ hàm kiểm tra và cấp huy hiệu cho người dùng
public async Task CheckAndAwardBadges(int userId)
{
    var user = await _context.Users
        .Include(u => u.BadgeAssignments)
        .FirstOrDefaultAsync(u => u.UserId == userId);
        
    if (user == null)
        return;
        
    var alreadyAwardedBadgeIds = user.BadgeAssignments.Select(ba => ba.BadgeId).ToList();
    
    // Kiểm tra huy hiệu First Question
    if (!alreadyAwardedBadgeIds.Contains((int)BadgeType.FirstQuestion))
    {
        var questionCount = await _context.Questions.CountAsync(q => q.UserId == userId);
        if (questionCount >= 1)
        {
            await AwardBadge(userId, (int)BadgeType.FirstQuestion);
        }
    }
    
    // Kiểm tra huy hiệu Popular Answer
    if (!alreadyAwardedBadgeIds.Contains((int)BadgeType.PopularAnswer))
    {
        var hasPopularAnswer = await _context.Answers
            .AnyAsync(a => a.UserId == userId && a.VoteCount >= 10);
            
        if (hasPopularAnswer)
        {
            await AwardBadge(userId, (int)BadgeType.PopularAnswer);
        }
    }
    
    // Thêm các kiểm tra huy hiệu khác...
}
```

### 📱 Tính Năng Real-time với SignalR
- **Instant Notifications**: Thông báo thời gian thực
- **Live Chat**: Tin nhắn trực tiếp giữa người dùng
- **Live Updates**: Cập nhật trạng thái câu hỏi, bình luận không cần refresh

**Cách triển khai**: Các hub SignalR như `NotificationHub`, `ChatHub`, và `QuestionHub` cung cấp kết nối WebSocket cho trải nghiệm real-time.

```csharp
// Ví dụ NotificationHub để gửi thông báo real-time
public class NotificationHub : Hub
{
    private readonly INotificationService _notificationService;
    
    public NotificationHub(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }
    
    public async Task SendNotification(int userId, string message, string type)
    {
        var notification = await _notificationService.CreateNotification(userId, message, type);
        await Clients.User(userId.ToString()).SendAsync("ReceiveNotification", notification);
    }
    
    public async Task MarkAsRead(int notificationId)
    {
        await _notificationService.MarkAsRead(notificationId);
        await Clients.Caller.SendAsync("NotificationMarkedAsRead", notificationId);
    }
}
```

## 🧠 Kiến Thức Mới và Kỹ Thuật Đáng Chú Ý

### 1. Model-View-Service Architecture
Dự án sử dụng kiến trúc hướng dịch vụ, tách biệt logic nghiệp vụ vào các service riêng biệt thay vì đặt trực tiếp trong controller, giúp code dễ bảo trì và test hơn.

### 2. Real-time Web với SignalR
SignalR được sử dụng để xây dựng các tính năng real-time như chat và thông báo, thay vì phụ thuộc vào polling truyền thống.

### 3. Repository Pattern và Unit of Work
Sử dụng pattern này để truy cập dữ liệu, tách biệt logic truy cập DB khỏi business logic.

### 4. Gitea API Integration
Tích hợp với Gitea API để quản lý mã nguồn, tạo trải nghiệm Git hoàn chỉnh mà không cần rời khỏi nền tảng.

### 5. Advanced Entity Framework Techniques
- Lazy loading và eager loading
- Shadow properties
- Query projections

### 6. Background Services trong ASP.NET Core
`NotificationBackgroundService` chạy ngầm để xử lý thông báo định kỳ và các tác vụ theo lịch.

### 7. Advanced Dependency Injection
Sử dụng extension methods để tổ chức việc đăng ký services vào DI container một cách có cấu trúc.

### 8. Markdown Rendering và Code Syntax Highlighting
Sử dụng Markdig để render Markdown và PrismJS cho syntax highlighting để hiển thị mã nguồn.

## 🚀 Hướng Dẫn Cài Đặt

### Yêu Cầu Hệ Thống
- .NET 9.0 SDK
- SQL Server 2019 trở lên
- Visual Studio 2022 (khuyến nghị) hoặc VS Code
- Gitea server (cho tính năng quản lý mã nguồn)

### Các Bước Cài Đặt

1. Clone repository:
```bash
git clone https://github.com/your-username/devcommunity.git
cd devcommunity
```

2. Cài đặt dependencies:
```bash
dotnet restore
```

3. Cấu hình database connection string trong `appsettings.json`

4. Chạy migrations để tạo database:
```bash
dotnet ef database update
```

5. Khởi chạy ứng dụng:
```bash
dotnet run
```

Hoặc mở solution trong Visual Studio và chạy từ IDE.

## 🔜 Kế Hoạch Phát Triển Tiếp Theo
- Cải thiện hệ thống tìm kiếm với Elasticsearch
- Thêm tính năng analytics và báo cáo
- Mở rộng API cho phép tích hợp bên thứ ba
- Tối ưu hiệu năng cho lượng dữ liệu lớn

---

## 👨‍💻 Đóng Góp
DevCommunity là dự án mã nguồn mở, chúng tôi rất hoan nghênh sự đóng góp từ cộng đồng. Hãy fork repository, tạo branch mới và gửi pull request để đóng góp.

---

<p align="center">DevCommunity - Nơi chia sẻ kiến thức và kết nối cộng đồng lập trình viên 💻</p> 