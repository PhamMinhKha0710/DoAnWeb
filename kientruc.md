# DevCommunity - Phân Tích Kiến Trúc & Chi Tiết Triển Khai

## 🏗️ Tổng Quan Kiến Trúc Dự Án

DevCommunity được xây dựng trên kiến trúc **N-tier** kết hợp với **Domain-Driven Design**, tập trung vào tính mô-đun hóa và separation of concerns. Kiến trúc này bao gồm:

1. **Presentation Layer**: Controllers, Views, ViewModels
2. **Business Logic Layer**: Services, Hubs
3. **Data Access Layer**: Repositories, DbContext
4. **Integration Layer**: GitIntegration, External Services
5. **Cross-Cutting Concerns**: Middleware, Filters, Extensions, Utils

## 📂 Phân Tích Chi Tiết Các Thư Mục

### 1. Controllers
```
Controllers/
├── AccountController.cs (52KB)
├── AnswersController.cs (3.5KB)
├── ApiController.cs (11KB)
├── BadgeController.cs (3.3KB)
├── ChatController.cs (7.4KB)
├── CommentsController.cs (12KB)
├── DiagnosticsController.cs (8.5KB)
├── HomeController.cs (5.7KB)
├── NotificationsController.cs (6.5KB)
├── ProfileController.cs (1.2KB)
├── QuestionsController.cs (35KB)
├── RepositoryController.cs (55KB)
├── SavedItemsController.cs (4.2KB)
├── TagPreferencesController.cs (7.7KB)
├── TagsController.cs (7.3KB)
├── UsersController.cs (7.5KB)
├── VoteController.cs (21KB)
└── Api/ (REST API endpoints)
```

**Vai trò**: Xử lý các HTTP requests, điều phối luồng dữ liệu giữa client và server, và chọn view phù hợp để render.

**Điểm nổi bật**:
- **AccountController.cs**: Quản lý xác thực, đăng ký, đăng nhập, OAuth integration với Google và GitHub
- **QuestionsController.cs**: Xử lý tất cả nghiệp vụ liên quan đến câu hỏi, bao gồm đăng câu hỏi, hiển thị, tìm kiếm, lọc theo tag
- **RepositoryController.cs**: Tích hợp với Gitea, quản lý repository, file và commit
- **VoteController.cs**: Xử lý upvote/downvote cho câu hỏi, câu trả lời và tính điểm uy tín

**Pattern sử dụng**: Controller → Service → Repository, với việc tách biệt rõ ràng giữa presentation logic và business logic.

### 2. Services
```
Services/
├── AnswerService.cs (14KB)
├── BadgeService.cs (13KB)
├── IAnswerService.cs (1.4KB)
├── IBadgeService.cs (1.8KB)
├── IMarkdownService.cs (542B)
├── INotificationService.cs (2.0KB)
├── IQuestionService.cs (13KB)
├── IRepositoryMappingService.cs (1.2KB)
├── IRepositoryService.cs (1.5KB)
├── IUserService.cs (2.1KB)
├── MarkdownService.cs (9.2KB)
├── NotificationBackgroundService.cs (5.8KB)
├── NotificationService.cs (14KB)
├── PasswordHashService.cs (6.7KB)
├── QuestionRealTimeService.cs (10KB)
├── QuestionService.cs (44KB)
├── RepositoryMappingService.cs (3.9KB)
├── RepositoryService.cs (11KB)
├── ReputationService.cs (8.9KB)
└── UserService.cs (18KB)
```

**Vai trò**: Chứa tất cả business logic của ứng dụng, tách biệt khỏi presentation layer.

**Điểm nổi bật**:
- **Interface-based design**: Mỗi service đều có interface tương ứng để hỗ trợ Dependency Injection và Unit Testing
- **MarkdownService.cs**: Xử lý render Markdown với sanitization để ngăn XSS
- **QuestionService.cs**: Phức tạp nhất (44KB), xử lý tất cả các nghiệp vụ liên quan đến câu hỏi
- **NotificationBackgroundService.cs**: Dịch vụ chạy nền để xử lý thông báo định kỳ, kế thừa từ BackgroundService của ASP.NET Core
- **ReputationService.cs**: Quản lý điểm uy tín và cấp huy hiệu dựa trên hoạt động

**Pattern sử dụng**: Command Pattern, Strategy Pattern và Observer Pattern kết hợp với Dependency Injection.

### 3. Repositories
```
Repositories/
├── CommentRepository.cs (884B)
├── ICommentRepository.cs (436B)
├── IPostRepository.cs (403B)
├── IQuestionRepository.cs (716B)
├── IRepository.cs (1.1KB)
├── IRepositoryMappingRepository.cs (718B)
├── IRepositoryRepository.cs (847B)
├── IUserRepository.cs (667B)
├── IUserSavedItemRepository.cs (873B)
├── PostRepository.cs (938B)
├── QuestionRepository.cs (1.8KB)
├── Repository.cs (2.6KB)
├── RepositoryMappingRepository.cs (1.1KB)
├── RepositoryRepository.cs (2.1KB)
├── UserRepository.cs (2.1KB)
└── UserSavedItemRepository.cs (3.1KB)
```

**Vai trò**: Trừu tượng hóa việc truy cập dữ liệu, tách biệt business logic khỏi data access logic.

**Điểm nổi bật**:
- **Generic Repository Pattern**: `IRepository<T>` và `Repository<T>` là base class cho tất cả repository cụ thể
- **Specialized Repositories**: Các repository chuyên biệt mở rộng thêm các phương thức đặc thù cho từng entity
- **Query Optimization**: Các repository có trách nhiệm tối ưu các truy vấn Entity Framework, giảm tải cho database

**Pattern sử dụng**: Repository Pattern kết hợp Unit of Work, tách biệt data access logic, giúp code dễ test và bảo trì.

### 4. Hubs
```
Hubs/
├── ActivityHub.cs (5.3KB)
├── BadgeHub.cs (2.4KB)
├── ChatHub.cs (16KB)
├── NotificationHub.cs (9.5KB)
├── PresenceHub.cs (6.0KB)
├── QuestionHub.cs (5.7KB)
└── ViewCountHub.cs (3.9KB)
```

**Vai trò**: Xử lý các kết nối real-time giữa client và server sử dụng SignalR.

**Điểm nổi bật**:
- **ChatHub.cs**: Lớn nhất (16KB), xử lý tin nhắn trực tiếp giữa các người dùng, hỗ trợ tin nhắn 1-1 và group chat
- **NotificationHub.cs**: Gửi thông báo real-time đến người dùng
- **QuestionHub.cs**: Cập nhật trạng thái câu hỏi, câu trả lời và bình luận real-time
- **PresenceHub.cs**: Theo dõi trạng thái online/offline của người dùng
- **BadgeHub.cs**: Thông báo khi người dùng đạt được huy hiệu mới

**Pattern sử dụng**: Observer Pattern và Pub/Sub Pattern, kết hợp với WebSocket để tạo kết nối hai chiều.

### 5. GitIntegration
```
GitIntegration/
├── CheckGiteaUsers.cs (5.5KB)
├── GiteaModels.cs (2.5KB)
├── GiteaRepositoryService.cs (22KB)
├── GiteaService.cs (48KB)
├── GiteaUserSyncService.cs (20KB)
├── IGiteaIntegrationService.cs (3.2KB)
├── IGiteaRepositoryService.cs (2.6KB)
├── Models/ (Entity mappings)
├── README.md (5.0KB)
├── SimpleGiteaService.cs (9.8KB)
├── setup-gitea.ps1 (6.9KB)
└── update-database.bat (2.8KB)
```

**Vai trò**: Tích hợp với Gitea để quản lý repository mã nguồn.

**Điểm nổi bật**:
- **GiteaService.cs**: Core service (48KB) tương tác với Gitea API, hỗ trợ tất cả các thao tác Git cơ bản
- **GiteaUserSyncService.cs**: Đồng bộ hóa người dùng giữa DevCommunity và Gitea
- **GiteaRepositoryService.cs**: Quản lý repository, branch, commit, và file
- **setup-gitea.ps1**: Script PowerShell để cài đặt và cấu hình Gitea server
- **Models/**: Chứa các model ánh xạ giữa entity của DevCommunity và Gitea

**Technical approach**:
- Sử dụng HttpClient để giao tiếp với Gitea API
- JWT authentication với Gitea API
- Đồng bộ hóa giữa local database và Gitea database

### 6. ViewModels
```
ViewModels/
├── ForgotPasswordViewModel.cs (350B)
├── LinkGiteaViewModel.cs (444B)
├── LoginViewModel.cs (446B)
├── ProfileViewModel.cs (2.0KB)
├── QuestionModel.cs (375B)
├── QuestionViewModel.cs (3.2KB)
├── RegisterViewModel.cs (1.4KB)
├── RepositoryExtendedViewModels.cs (6.9KB)
├── RepositoryFileViewModel.cs (662B)
├── RepositoryViewModel.cs (785B)
├── RepositoryViewModels.cs (2.9KB)
└── ResetPasswordViewModel.cs (956B)
```

**Vai trò**: Data Transfer Objects (DTOs) để chuyển dữ liệu giữa controller và view, cũng như giữa các layer.

**Điểm nổi bật**:
- **QuestionViewModel.cs**: Model phức tạp cho các view liên quan đến câu hỏi
- **RepositoryExtendedViewModels.cs**: Tập hợp các ViewModel cho tính năng repository
- **ProfileViewModel.cs**: Dữ liệu cho trang hồ sơ người dùng, bao gồm hoạt động, huy hiệu, và điểm uy tín

**Pattern sử dụng**:
- DTO Pattern: Tránh truyền domain model trực tiếp ra view
- View Model Pattern: Cung cấp dữ liệu đã định dạng phù hợp cho view

### 7. Views
```
Views/
├── Account/ (Đăng nhập, đăng ký)
├── Chat/ (Tin nhắn)
├── Home/ (Trang chủ)
├── Notifications/ (Thông báo)
├── Questions/ (Hỏi đáp)
├── Repository/ (Code repository)
├── SavedItems/ (Bookmark)
├── Shared/ (Layout, partial views)
├── TagPreferences/ (Tag preferences)
├── Tags/ (Quản lý tag)
├── Users/ (Hồ sơ người dùng)
├── _ViewImports.cshtml
└── _ViewStart.cshtml
```

**Vai trò**: Chứa các Razor View để render HTML UI cho người dùng.

**Điểm nổi bật**:
- **Shared/**: Chứa layout, components và partial views dùng chung
- **Questions/**: Views cho các chức năng liên quan đến câu hỏi và câu trả lời
- **Repository/**: Views cho quản lý và hiển thị repository code

**Kỹ thuật sử dụng**:
- Razor syntax với C# embedding
- Layout templates và partial views
- View Components cho các UI elements tái sử dụng

### 8. Middleware
```
Middleware/
└── SavedItemsMiddleware.cs (3.0KB)
```

**Vai trò**: Xử lý request pipeline, thực hiện các tác vụ trước hoặc sau khi controller xử lý request.

**Điểm nổi bật**:
- **SavedItemsMiddleware.cs**: Theo dõi và xử lý các bookmark của người dùng

**Cách triển khai**:
```csharp
public class SavedItemsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IUserSavedItemRepository _savedItemRepository;

    public SavedItemsMiddleware(RequestDelegate next, IUserSavedItemRepository savedItemRepository)
    {
        _next = next;
        _savedItemRepository = savedItemRepository;
    }

    public async Task InvokeAsync(HttpContext context, IUserService userService)
    {
        // Xử lý trước khi đi đến controller
        if (context.User.Identity.IsAuthenticated)
        {
            var userId = userService.GetCurrentUserId();
            context.Items["SavedItemsCount"] = await _savedItemRepository.GetUserSavedItemsCount(userId);
        }

        // Chuyển request đến middleware tiếp theo
        await _next(context);
        
        // Xử lý sau khi controller hoàn thành
    }
}
```

### 9. Extensions
```
Extensions/
├── ApplicationExtensions.cs (2.4KB)
├── DatabaseMigrationExtensions.cs (12KB)
├── MiddlewareExtensions.cs (424B)
└── ServiceExtensions/ (Extension methods cho DI)
```

**Vai trò**: Mở rộng các class có sẵn với các phương thức tiện ích.

**Điểm nổi bật**:
- **DatabaseMigrationExtensions.cs**: Tự động chạy migration khi ứng dụng khởi động
- **ServiceExtensions/**: Tổ chức việc đăng ký service trong Dependency Injection container

**Cách triển khai**:
```csharp
// Ví dụ từ MiddlewareExtensions.cs
public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseCustomExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
    
    public static IApplicationBuilder UseSavedItemsMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SavedItemsMiddleware>();
    }
}
```

## 🔍 Chi Tiết Triển Khai Các Chức Năng

### 1. Hệ Thống Xác Thực & Phân Quyền

**Thành phần chính**:
- AccountController.cs
- UserService.cs
- PasswordHashService.cs
- LoginViewModel.cs, RegisterViewModel.cs

**Cách triển khai**:
```csharp
// AccountController.cs
[HttpPost]
public async Task<IActionResult> Register(RegisterViewModel model)
{
    if (ModelState.IsValid)
    {
        // Kiểm tra email đã tồn tại chưa
        if (await _userService.EmailExists(model.Email))
        {
            ModelState.AddModelError("Email", "Email đã được sử dụng");
            return View(model);
        }

        // Tạo user mới
        var user = new User
        {
            Username = model.Username,
            Email = model.Email,
            PasswordHash = _passwordHashService.HashPassword(model.Password),
            RegistrationDate = DateTime.UtcNow,
            LastLoginDate = DateTime.UtcNow,
            IsActive = true
        };

        // Lưu vào database
        await _userService.CreateUser(user);

        // Đăng nhập người dùng
        await _signInManager.SignInAsync(user, isPersistent: false);

        // Tạo tài khoản Gitea tương ứng
        await _giteaUserSyncService.CreateGiteaUser(user);

        return RedirectToAction("Index", "Home");
    }

    return View(model);
}
```

### 2. Hệ Thống Q&A với Real-time Updates

**Thành phần chính**:
- QuestionsController.cs
- QuestionService.cs
- QuestionHub.cs
- MarkdownService.cs
- VoteController.cs

**Cách triển khai**:
```csharp
// QuestionsController.cs
[HttpPost]
[Authorize]
public async Task<IActionResult> Create(QuestionViewModel model)
{
    if (ModelState.IsValid)
    {
        var userId = _userService.GetCurrentUserId();
        
        // Sanitize and parse markdown
        var sanitizedContent = _markdownService.SanitizeAndRenderMarkdown(model.Content);
        
        var question = new Question
        {
            Title = model.Title,
            Content = model.Content,
            SanitizedContent = sanitizedContent,
            UserId = userId,
            CreatedDate = DateTime.UtcNow,
            LastActivityDate = DateTime.UtcNow
        };
        
        // Xử lý tags
        if (!string.IsNullOrEmpty(model.Tags))
        {
            var tagNames = model.Tags.Split(',')
                .Select(t => t.Trim().ToLower())
                .Where(t => !string.IsNullOrEmpty(t))
                .ToList();
                
            await _questionService.AddQuestionWithTags(question, tagNames);
        }
        else
        {
            await _questionService.AddQuestion(question);
        }
        
        // Gửi thông báo real-time
        await _questionHub.Clients.All.SendAsync(
            "NewQuestion", 
            new { id = question.QuestionId, title = question.Title });
            
        // Kiểm tra và cấp huy hiệu
        await _badgeService.CheckAndAwardQuestionBadges(userId);
        
        return RedirectToAction("Details", new { id = question.QuestionId });
    }
    
    return View(model);
}
```

### 3. Tích Hợp Git với Gitea

**Thành phần chính**:
- RepositoryController.cs
- GiteaService.cs
- GiteaRepositoryService.cs
- GiteaUserSyncService.cs

**Cách triển khai**:
```csharp
// RepositoryController.cs
[HttpPost]
[Authorize]
public async Task<IActionResult> Create(CreateRepositoryViewModel model)
{
    if (ModelState.IsValid)
    {
        var userId = _userService.GetCurrentUserId();
        var user = await _userService.GetUserById(userId);
        
        // Tạo repo trong Gitea
        var giteaRepo = await _giteaService.CreateRepository(
            user.Username, 
            model.Name, 
            model.Description, 
            model.IsPrivate);
            
        if (giteaRepo != null)
        {
            // Lưu mapping giữa repo Gitea và local database
            var repository = new Repository
            {
                Name = model.Name,
                Description = model.Description,
                UserId = userId,
                IsPrivate = model.IsPrivate,
                CreatedDate = DateTime.UtcNow,
                LastActivityDate = DateTime.UtcNow
            };
            
            await _repositoryService.AddRepository(repository);
            
            // Tạo mapping với Gitea repo
            await _repositoryMappingService.CreateMapping(
                repository.RepositoryId, 
                giteaRepo.Id, 
                user.Username, 
                model.Name);
                
            // Tạo README.md mặc định
            if (model.InitializeWithReadme)
            {
                var readmeContent = $"# {model.Name}\n\n{model.Description}";
                await _giteaService.CreateFile(
                    user.Username,
                    model.Name,
                    "README.md",
                    readmeContent,
                    "Initial commit with README");
            }
            
            return RedirectToAction("Details", new { id = repository.RepositoryId });
        }
        else
        {
            ModelState.AddModelError("", "Không thể tạo repository trong Gitea");
        }
    }
    
    return View(model);
}
```

### 4. Hệ Thống Huy Hiệu và Danh Hiệu

**Thành phần chính**:
- BadgeController.cs
- BadgeService.cs
- BadgeHub.cs
- ReputationService.cs
- BadgeProgress.cs

**Cách triển khai**:
```csharp
// BadgeService.cs
public async Task CheckAndAwardQuestionBadges(int userId)
{
    var user = await _context.Users
        .Include(u => u.BadgeAssignments)
        .FirstOrDefaultAsync(u => u.UserId == userId);
        
    if (user == null)
        return;
        
    var alreadyAwardedBadgeIds = user.BadgeAssignments
        .Select(ba => ba.BadgeId)
        .ToList();
        
    // First Question Badge
    if (!alreadyAwardedBadgeIds.Contains((int)BadgeType.FirstQuestion))
    {
        var questionCount = await _context.Questions.CountAsync(q => q.UserId == userId);
        if (questionCount >= 1)
        {
            await AwardBadge(userId, (int)BadgeType.FirstQuestion);
        }
    }
    
    // Question Master Badge
    if (!alreadyAwardedBadgeIds.Contains((int)BadgeType.QuestionMaster))
    {
        var questionCount = await _context.Questions.CountAsync(q => q.UserId == userId);
        if (questionCount >= 50)
        {
            await AwardBadge(userId, (int)BadgeType.QuestionMaster);
        }
    }
    
    // Popular Question Badge - Check for questions with 10+ upvotes
    if (!alreadyAwardedBadgeIds.Contains((int)BadgeType.PopularQuestion))
    {
        var hasPopularQuestion = await _context.Questions
            .AnyAsync(q => q.UserId == userId && q.VoteCount >= 10);
            
        if (hasPopularQuestion)
        {
            await AwardBadge(userId, (int)BadgeType.PopularQuestion);
        }
    }
    
    // Cập nhật tiến trình các huy hiệu đang trong quá trình đạt được
    await UpdateBadgeProgress(userId);
}
```

## 🎯 Triển Khai Chức Năng Nâng Cao

### 1. Tổ Chức Frontend Assets (wwwroot)
```
wwwroot/
├── css/ (Stylesheets)
├── js/ (JavaScript files)
├── images/ (Image assets)
├── lib/ (Client-side libraries)
├── uploads/ (User uploads)
├── audio/ (Audio files)
└── favicon.ico
```

**Vai trò**: Chứa các static files như CSS, JavaScript, hình ảnh, và thư viện client-side.

**Điểm nổi bật**:
- **js/signalr/**: Client libraries cho SignalR
- **js/markdown-editor.js**: Custom markdown editor
- **js/code-viewer.js**: Syntax highlighting cho code snippets
- **css/themes/**: Các theme cho ứng dụng

**Cách triển khai**:
```javascript
// wwwroot/js/signalr-connection.js
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/notification")
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();

connection.on("ReceiveNotification", (notification) => {
    // Add notification to UI
    const notificationList = document.getElementById("notificationList");
    const notificationItem = document.createElement("div");
    notificationItem.className = "notification-item";
    notificationItem.innerHTML = `
        <div class="notification-content">
            <span class="notification-message">${notification.message}</span>
            <span class="notification-time">${new Date(notification.createdDate).toLocaleTimeString()}</span>
        </div>
        <button class="mark-read-btn" data-id="${notification.id}">Mark as read</button>
    `;
    notificationList.prepend(notificationItem);
    
    // Update notification counter
    const counter = document.getElementById("notification-counter");
    counter.textContent = parseInt(counter.textContent || "0") + 1;
});

// Start connection
connection.start().catch(err => console.error(err));
```

### 2. ViewComponents Chi Tiết

ViewComponents là một tính năng mạnh mẽ của ASP.NET Core, cho phép tạo các UI component tái sử dụng với logic phức tạp hơn partial views.

**Cách hoạt động**:
1. ViewComponent class kế thừa từ ViewComponent base class
2. Implement phương thức Invoke hoặc InvokeAsync
3. Trả về view riêng của component
4. Có thể inject dependencies qua constructor

**Ví dụ triển khai**:
```csharp
// ViewComponents/PopularTagsViewComponent.cs
public class PopularTagsViewComponent : ViewComponent
{
    private readonly ITagService _tagService;
    
    public PopularTagsViewComponent(ITagService tagService)
    {
        _tagService = tagService;
    }
    
    public async Task<IViewComponentResult> InvokeAsync(int count = 10)
    {
        var popularTags = await _tagService.GetPopularTags(count);
        return View(popularTags);
    }
}
```

**Sử dụng trong Razor View**:
```cshtml
@* Views/Shared/Components/PopularTags/Default.cshtml *@
@model List<Tag>

<div class="popular-tags">
    <h5>Popular Tags</h5>
    <div class="tag-cloud">
        @foreach (var tag in Model)
        {
            <a href="@Url.Action("Index", "Questions", new { tag = tag.Name })" 
               class="tag-item" title="@tag.Description">
                @tag.Name 
                <span class="tag-count">×@tag.UsageCount</span>
            </a>
        }
    </div>
</div>
```

**Cách gọi trong Views**:
```cshtml
@await Component.InvokeAsync("PopularTags", new { count = 20 })
```

### 3. Hệ Thống Chat Real-time

**Thành phần chính**:
- ChatHub.cs
- ChatController.cs
- Message.cs, Conversation.cs
- ConversationParticipant.cs

**Các tính năng**:
- Tin nhắn 1-1 và group chats
- Hiển thị trạng thái online/offline
- Thông báo typing
- Lưu trữ lịch sử chat
- Đánh dấu tin nhắn đã đọc

**Cách triển khai**:
```csharp
// ChatHub.cs
public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly IUserService _userService;
    private static readonly ConcurrentDictionary<string, UserConnection> _connections = 
        new ConcurrentDictionary<string, UserConnection>();
    
    public ChatHub(IChatService chatService, IUserService userService)
    {
        _chatService = chatService;
        _userService = userService;
    }
    
    public async Task SendMessage(int conversationId, string message)
    {
        if (Context.User.Identity.IsAuthenticated)
        {
            var userId = _userService.GetUserIdFromUserClaims(Context.User);
            var user = await _userService.GetUserById(userId);
            
            // Check if user is part of conversation
            var isParticipant = await _chatService.IsConversationParticipant(conversationId, userId);
            if (!isParticipant)
                return;
                
            // Save message to database
            var savedMessage = await _chatService.SaveMessage(conversationId, userId, message);
            
            // Send to everyone in the conversation except sender
            await Clients
                .Group($"Conversation_{conversationId}")
                .SendAsync("ReceiveMessage", new 
                {
                    messageId = savedMessage.MessageId,
                    senderId = userId,
                    senderName = user.Username,
                    content = savedMessage.Content,
                    timestamp = savedMessage.SentDate.ToString("yyyy-MM-dd HH:mm:ss")
                });
                
            // Send confirmation to sender
            await Clients.Caller.SendAsync("MessageSent", savedMessage.MessageId);
        }
    }
    
    public async Task JoinConversation(int conversationId)
    {
        if (Context.User.Identity.IsAuthenticated)
        {
            var userId = _userService.GetUserIdFromUserClaims(Context.User);
            
            // Check if user is part of conversation
            var isParticipant = await _chatService.IsConversationParticipant(conversationId, userId);
            if (!isParticipant)
                return;
                
            // Add user to SignalR group for this conversation
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Conversation_{conversationId}");
            
            // Mark messages as read
            await _chatService.MarkMessagesAsRead(conversationId, userId);
            
            // Notify others that user joined
            await Clients
                .OthersInGroup($"Conversation_{conversationId}")
                .SendAsync("UserJoined", userId);
        }
    }
    
    public async Task SendTypingNotification(int conversationId)
    {
        if (Context.User.Identity.IsAuthenticated)
        {
            var userId = _userService.GetUserIdFromUserClaims(Context.User);
            var user = await _userService.GetUserById(userId);
            
            await Clients
                .OthersInGroup($"Conversation_{conversationId}")
                .SendAsync("UserTyping", user.Username);
        }
    }
}
```

### 4. Advanced Tag System

**Thành phần chính**:
- TagsController.cs
- Tag.cs, QuestionTag.cs
- UserWatchedTag.cs, UserIgnoredTag.cs

**Tính năng**:
- Tag management (create, edit, merge, synonyms)
- Tag preferences (watched tags, ignored tags)
- Tag-based notifications
- Tag-based filtering
- Tag badges

**Cách triển khai**:
```csharp
// TagSystem/TagService.cs
public class TagService : ITagService
{
    private readonly DevCommunityContext _context;
    private readonly IMemoryCache _cache;
    
    public TagService(DevCommunityContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }
    
    public async Task<List<Tag>> GetPopularTags(int count)
    {
        // Try get from cache first
        string cacheKey = $"PopularTags_{count}";
        if (_cache.TryGetValue(cacheKey, out List<Tag> cachedTags))
        {
            return cachedTags;
        }
        
        // Query database
        var popularTags = await _context.Tags
            .OrderByDescending(t => t.UsageCount)
            .Take(count)
            .ToListAsync();
            
        // Store in cache for 15 minutes
        _cache.Set(cacheKey, popularTags, TimeSpan.FromMinutes(15));
        
        return popularTags;
    }
    
    public async Task<List<Tag>> GetUserWatchedTags(int userId)
    {
        return await _context.UserWatchedTags
            .Where(uwt => uwt.UserId == userId)
            .Include(uwt => uwt.Tag)
            .Select(uwt => uwt.Tag)
            .ToListAsync();
    }
    
    public async Task<bool> WatchTag(int userId, int tagId)
    {
        // Check if already watching
        var existingWatch = await _context.UserWatchedTags
            .FirstOrDefaultAsync(uwt => uwt.UserId == userId && uwt.TagId == tagId);
            
        if (existingWatch != null)
            return false;
            
        // Remove from ignored if applicable
        var ignoredTag = await _context.UserIgnoredTags
            .FirstOrDefaultAsync(uit => uit.UserId == userId && uit.TagId == tagId);
            
        if (ignoredTag != null)
        {
            _context.UserIgnoredTags.Remove(ignoredTag);
        }
        
        // Add to watched tags
        _context.UserWatchedTags.Add(new UserWatchedTag 
        {
            UserId = userId,
            TagId = tagId,
            CreatedDate = DateTime.UtcNow
        });
        
        await _context.SaveChangesAsync();
        return true;
    }
    
    public async Task<List<Question>> GetQuestionsForTags(List<int> tagIds, int page = 1, int pageSize = 20)
    {
        var questions = await _context.Questions
            .Where(q => q.QuestionTags.Any(qt => tagIds.Contains(qt.TagId)))
            .Include(q => q.User)
            .Include(q => q.QuestionTags)
                .ThenInclude(qt => qt.Tag)
            .OrderByDescending(q => q.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
            
        return questions;
    }
    
    // Other tag management methods...
}
```

### 5. Search System

DevCommunity triển khai hệ thống tìm kiếm mạnh mẽ cho câu hỏi, câu trả lời, và mã nguồn.

**Tính năng**:
- Full-text search
- Tìm kiếm theo tags
- Tìm kiếm theo người dùng
- Sắp xếp kết quả theo relevance, newest, votes
- Bộ lọc nâng cao

**Cách triển khai**:
```csharp
// SearchService.cs
public class SearchService : ISearchService
{
    private readonly DevCommunityContext _context;
    
    public SearchService(DevCommunityContext context)
    {
        _context = context;
    }
    
    public async Task<SearchResult> Search(SearchQuery query)
    {
        var result = new SearchResult();
        
        // Base query
        var questionsQuery = _context.Questions
            .Include(q => q.User)
            .Include(q => q.QuestionTags)
                .ThenInclude(qt => qt.Tag)
            .AsQueryable();
            
        // Apply text search
        if (!string.IsNullOrEmpty(query.Text))
        {
            questionsQuery = questionsQuery.Where(q => 
                q.Title.Contains(query.Text) || 
                q.Content.Contains(query.Text));
        }
        
        // Apply tag filter
        if (query.Tags?.Count > 0)
        {
            questionsQuery = questionsQuery.Where(q => 
                q.QuestionTags.Any(qt => query.Tags.Contains(qt.Tag.Name)));
        }
        
        // Apply user filter
        if (query.UserId.HasValue)
        {
            questionsQuery = questionsQuery.Where(q => q.UserId == query.UserId.Value);
        }
        
        // Apply sort
        switch (query.SortBy)
        {
            case "newest":
                questionsQuery = questionsQuery.OrderByDescending(q => q.CreatedDate);
                break;
            case "votes":
                questionsQuery = questionsQuery.OrderByDescending(q => q.VoteCount);
                break;
            case "activity":
                questionsQuery = questionsQuery.OrderByDescending(q => q.LastActivityDate);
                break;
            case "relevance":
            default:
                // For relevance, we would ideally use full-text search
                // But in SQL Server, we can approximate with CONTAINS/FREETEXT
                questionsQuery = questionsQuery.OrderByDescending(q => q.VoteCount)
                    .ThenByDescending(q => q.AnswerCount)
                    .ThenByDescending(q => q.ViewCount);
                break;
        }
        
        // Get total count
        result.TotalCount = await questionsQuery.CountAsync();
        
        // Apply pagination
        var pageSize = query.PageSize ?? 20;
        var page = query.Page ?? 1;
        
        result.Questions = await questionsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
            
        result.CurrentPage = page;
        result.PageSize = pageSize;
        
        return result;
    }
}
```

### 6. Bảo Mật và Authorization

DevCommunity có một hệ thống bảo mật và authorization toàn diện:

**Tính năng**:
- Role-based authorization (Admin, Moderator, User)
- Resource-based authorization (Owner of content)
- Password hashing với BCrypt
- Cross-Site Request Forgery (CSRF) protection
- Cross-Site Scripting (XSS) protection
- SQL Injection prevention via Entity Framework
- Rate limiting

**Cách triển khai**:

```csharp
// Filters/ResourceOwnerAuthorizationFilter.cs
public class ResourceOwnerAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly IUserService _userService;
    private readonly string _entityType;
    private readonly string _idParameterName;
    
    public ResourceOwnerAuthorizationFilter(
        IUserService userService, 
        string entityType,
        string idParameterName = "id")
    {
        _userService = userService;
        _entityType = entityType;
        _idParameterName = idParameterName;
    }
    
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Get current user
        if (!context.HttpContext.User.Identity.IsAuthenticated)
        {
            context.Result = new ChallengeResult();
            return;
        }
        
        // Get entity ID from route
        if (!context.RouteData.Values.TryGetValue(_idParameterName, out var idObj))
        {
            context.Result = new BadRequestResult();
            return;
        }
        
        if (!int.TryParse(idObj.ToString(), out var entityId))
        {
            context.Result = new BadRequestResult();
            return;
        }
        
        // Check if user is admin or moderator
        if (context.HttpContext.User.IsInRole("Admin") || 
            context.HttpContext.User.IsInRole("Moderator"))
        {
            return; // Allow
        }
        
        // Check if user is the owner
        var currentUserId = _userService.GetCurrentUserId();
        bool isOwner = _entityType switch
        {
            "Question" => await _userService.IsQuestionOwner(currentUserId, entityId),
            "Answer" => await _userService.IsAnswerOwner(currentUserId, entityId),
            "Comment" => await _userService.IsCommentOwner(currentUserId, entityId),
            "Repository" => await _userService.IsRepositoryOwner(currentUserId, entityId),
            _ => false
        };
        
        if (!isOwner)
        {
            context.Result = new ForbidResult();
        }
    }
}
```

**Sử dụng trên Controller Action**:
```csharp
[HttpPost]
[Authorize]
[ServiceFilter(typeof(ResourceOwnerAuthorizationFilter), Arguments = new object[] { "Question" })]
public async Task<IActionResult> Delete(int id)
{
    await _questionService.DeleteQuestion(id);
    return RedirectToAction("Index");
}
```

## 🔄 Luồng Dữ Liệu và Request Pipeline

### Request Pipeline
```
Client Request → 
  Middleware (Exception handling, Authentication) → 
    Routing → 
      Action Filters → 
        Controller Action → 
          Service → 
            Repository → 
              Database → 
            Repository → 
          Service → 
        Controller → 
      View → 
    Response Formatting → 
  Middleware → 
Client Response
```

### Data Flow Patterns

1. **Repository Pattern**: Abstraction layer giữa business logic và data persistence.
   ```
   Controller → Service → Repository → DbContext
   ```

2. **Command Query Separation**: Tách riêng các hoạt động đọc (query) và ghi (command).
   ```
   // Query
   Controller → Service → Repository.GetAll() → DbContext

   // Command
   Controller → Service → Repository.Add() → DbContext.SaveChanges()
   ```

3. **Event-Driven Architecture**: Sử dụng SignalR hubs để truyền sự kiện real-time.
   ```
   User Action → Controller → Service → Database → Hub → Clients
   ```

### Performance Optimization

DevCommunity triển khai nhiều cách tối ưu hiệu suất:

1. **Database Query Optimization**
   - Eager Loading với Include()
   - Indexing
   - Query batching

2. **Caching**
   - In-memory caching cho dữ liệu thường xuyên sử dụng
   - Response caching cho các trang ít thay đổi
   - Entity Framework query caching

3. **Async/Await Pattern**
   - Tất cả I/O operations sử dụng async để giải phóng thread pool

## 📝 Tổng Kết

DevCommunity là một ứng dụng web hiện đại với kiến trúc đa tầng rõ ràng, áp dụng nhiều nguyên tắc thiết kế phần mềm tiên tiến. Dự án đã thành công trong việc kết hợp nhiều công nghệ và pattern khác nhau:

1. **ASP.NET Core MVC + Razor Views** cho presentation layer
2. **Entity Framework Core** cho data access
3. **SignalR** cho real-time communication
4. **Repository Pattern** cho data abstraction
5. **Service Layer** cho business logic
6. **Dependency Injection** để giảm coupling
7. **External API Integration** với Gitea

Với cách tiếp cận modular và decoupled, dự án có thể dễ dàng mở rộng và bảo trì theo thời gian, đồng thời cung cấp nền tảng vững chắc cho việc thêm các tính năng mới trong tương lai. 