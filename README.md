# 🚀 DevCommunity

## 💻 Cộng đồng chia sẻ mã nguồn & hỏi đáp lập trình

![DevCommunity Logo](https://github.com/user-attachments/assets/4d958968-526b-446a-9379-4e0231cd9ee8)

DevCommunity là nền tảng kết nối lập trình viên thông qua việc chia sẻ mã nguồn và hỏi đáp kỹ thuật. Dự án được xây dựng trên nền tảng ASP.NET Core, kết hợp các tính năng của GitHub và Stack Overflow để tạo môi trường học tập và chia sẻ kiến thức hiệu quả.

---

## ✨ Tính Năng Đã Triển Khai

### 👥 Quản Lý Người Dùng
- **Đăng ký** & **đăng nhập** với ASP.NET Identity
- **Xác thực OAuth** với Google và GitHub
- **Phân quyền** với hệ thống role-based (Admin, Moderator, User)
- Quản lý hồ sơ cá nhân với avatar, thông tin liên hệ
- Theo dõi hoạt động và đóng góp của người dùng

### ❓ Hệ Thống Hỏi Đáp
- Đăng câu hỏi với trình soạn thảo Markdown đầy đủ
- Hỗ trợ định dạng phong phú với trình soạn thảo trực quan
- Chức năng xem trước (Preview) nội dung Markdown
- Đính kèm tập tin đa dạng (hình ảnh, PDF, tài liệu, mã nguồn)
- Hệ thống tag để phân loại câu hỏi
- Bình chọn (Upvote/Downvote) cho câu hỏi và câu trả lời

### 📦 Quản Lý Mã Nguồn
- Tạo và quản lý repository công khai/riêng tư
- Quản lý file và thư mục với giao diện trực quan
- Hỗ trợ tạo file mới với nhiều template có sẵn
- Tích hợp với Gitea cho quản lý mã nguồn
- Xem mã nguồn với syntax highlighting

### 🔔 Thông Báo & Tương Tác Thời Gian Thực
- Thông báo realtime khi có tương tác mới
- Chat trực tiếp giữa người dùng
- Cập nhật trạng thái câu hỏi và bình luận không cần refresh
- Hiển thị người dùng trực tuyến

### 🏆 Hệ Thống Danh Hiệu & Uy Tín
- Huy hiệu thành tựu cho hoạt động tích cực
- Điểm uy tín dựa trên đóng góp và tương tác
- Cấp độ người dùng theo hoạt động

### 🔍 Tìm Kiếm & Phân Loại
- Hệ thống tag linh hoạt cho câu hỏi và repository
- Tìm kiếm nội dung theo nhiều tiêu chí
- Phân loại câu hỏi và mã nguồn theo danh mục
- Lưu mục yêu thích và theo dõi tag ưa thích

---

## 🛠️ Công Nghệ Sử Dụng

### 🖥️ Backend
- **ASP.NET Core 9.0**: Nền tảng phát triển ứng dụng web
- **Entity Framework Core 9.0**: ORM để tương tác với cơ sở dữ liệu
- **ASP.NET Identity**: Quản lý xác thực và phân quyền người dùng
- **SignalR**: Tính năng realtime và thông báo tức thì
- **Gitea API Integration**: Tích hợp với Gitea cho quản lý mã nguồn
- **Markdig**: Xử lý và render Markdown
- **BCrypt.Net-Next**: Bảo mật mật khẩu

### 🎨 Frontend
- **Bootstrap 5**: Framework CSS cho UI responsive
- **jQuery**: Thư viện JavaScript cho tương tác động
- **Marked.js**: Thư viện render Markdown
- **PrismJS**: Syntax highlighting cho code blocks
- **Bootstrap Icons**: Bộ icon vector đẹp và linh hoạt

### 💾 Cơ Sở Dữ Liệu
- **Microsoft SQL Server**: Hệ quản trị cơ sở dữ liệu quan hệ
- **Entity Framework Migrations**: Quản lý phiên bản database

### 📁 Tính Năng Tập Tin
- **Hỗ trợ upload đa dạng**: Hình ảnh, PDF, văn bản, mã nguồn
- **Image preview**: Xem trước hình ảnh trước khi đăng tải
- **File categorization**: Phân loại file theo định dạng

---

## 🌟 Hướng Phát Triển Tiếp Theo

- **Nâng cao hệ thống bình chọn**: Cải thiện trải nghiệm Upvote/Downvote
- **Tối ưu hóa thời gian thực**: Cải thiện hiệu suất SignalR
- **Hoàn thiện hệ thống thông báo**: Email và thông báo trong ứng dụng
- **Mở rộng hệ thống điểm uy tín**: Thêm nhiều cấp độ và phần thưởng
- **Nâng cao chức năng bình luận**: Threading và Markdown trong bình luận
- **Phát triển API công khai**: Cho phép tích hợp với các dịch vụ bên thứ ba
- **Tối ưu hóa hiệu suất**: Cải thiện tốc độ tải trang và phản hồi API

---

## 🚀 Cài Đặt và Chạy Dự Án

### Yêu Cầu Hệ Thống
- .NET 9.0 SDK trở lên
- SQL Server 2022 trở lên
- Visual Studio 2022 hoặc VS Code với C# extensions
- Gitea server (tùy chọn cho tính năng quản lý mã nguồn)

### Các Bước Cài Đặt

```bash
# Clone repository
git clone https://github.com/yourusername/devcommunity.git

# Di chuyển vào thư mục dự án
cd devcommunity

# Khôi phục các gói NuGet
dotnet restore

# Cập nhật database
dotnet ef database update

# Chạy ứng dụng
dotnet run
```

### Cấu Hình
1. Cập nhật chuỗi kết nối trong `appsettings.json`
2. Thiết lập các khóa OAuth cho Google/GitHub nếu muốn sử dụng đăng nhập bên thứ ba
3. Cấu hình Gitea nếu muốn sử dụng tính năng quản lý mã nguồn

### 🔄 Cài Đặt và Cấu Hình Gitea

Gitea là một dịch vụ Git nhẹ, tự host được sử dụng trong DevCommunity để quản lý mã nguồn. Dưới đây là các bước để cài đặt và tích hợp Gitea với ứng dụng:

#### 1. Tải và Cài Đặt Gitea

##### Windows:

```powershell
# Tạo thư mục cài đặt Gitea
mkdir C:\gitea

# Tải phiên bản mới nhất của Gitea
Invoke-WebRequest -Uri "https://dl.gitea.com/gitea/latest/gitea-latest-windows-4.0-amd64.exe" -OutFile "C:\gitea\gitea.exe"

# Tạo cấu trúc thư mục cần thiết
mkdir C:\gitea\custom
mkdir C:\gitea\data
mkdir C:\gitea\log
```

##### Linux/macOS:

```bash
# Tạo người dùng Gitea
sudo adduser --system --group --disabled-password --shell /bin/bash --home /home/gitea --gecos 'Gitea' gitea

# Tạo thư mục cần thiết
sudo mkdir -p /var/lib/gitea/{custom,data,log}
sudo chown -R gitea:gitea /var/lib/gitea
sudo chmod -R 750 /var/lib/gitea

# Tải Gitea
wget -O /tmp/gitea https://dl.gitea.com/gitea/latest/gitea-latest-linux-amd64
sudo install -m 755 /tmp/gitea /usr/local/bin/gitea
```

#### 2. Cài Đặt Cơ Sở Dữ Liệu cho Gitea

Gitea hỗ trợ nhiều loại cơ sở dữ liệu. Ví dụ với SQLite:

```bash
# Tạo thư mục cho database
mkdir -p /var/lib/gitea/data/gitea.db
```

#### 3. Cấu Hình và Chạy Gitea

##### Windows:

Tạo file `C:\gitea\custom\conf\app.ini` với nội dung:

```ini
[database]
DB_TYPE = sqlite3
PATH = C:/gitea/data/gitea.db

[server]
HTTP_PORT = 3000
ROOT_URL = http://localhost:3000/
```

Chạy Gitea:

```powershell
# Chạy Gitea với PowerShell
Start-Process -FilePath "C:\gitea\gitea.exe" -WorkingDirectory "C:\gitea"

# Hoặc thiết lập Gitea như một dịch vụ Windows
sc.exe create gitea start= auto binPath= "\"C:\gitea\gitea.exe\" web --config \"C:\gitea\custom\conf\app.ini\""
sc.exe start gitea
```

##### Linux/macOS:

```bash
# Tạo file cấu hình
sudo -u gitea nano /etc/gitea/app.ini

# Chạy Gitea
sudo -u gitea gitea web -c /etc/gitea/app.ini
```

#### 4. Thiết Lập Ban Đầu

1. Truy cập Gitea tại địa chỉ `http://localhost:3000`
2. Hoàn thành quá trình cài đặt qua giao diện web:
   - Chọn loại cơ sở dữ liệu (SQLite/MySQL/PostgreSQL)
   - Đặt URL của Gitea (mặc định: `http://localhost:3000/`)
   - Tạo tài khoản quản trị đầu tiên

#### 5. Tích Hợp Gitea với DevCommunity

1. Cập nhật cấu hình Gitea trong `appsettings.json`:

```json
"Gitea": {
  "BaseUrl": "http://localhost:3000",
  "ApiUrl": "http://localhost:3000/api/v1" 
},
"GiteaSettings": {
  "ServerUrl": "http://localhost:3000",
  "Scopes": "read,write,admin"
}
```

2. Tạo Access Token trong Gitea:
   - Đăng nhập vào Gitea
   - Vào Settings > Applications > Generate New Token
   - Đặt tên và quyền cho token
   - Sao chép token được tạo

3. Cấu hình ứng dụng Client OAuth trong Gitea:
   - Vào Settings > Applications > OAuth2 Applications
   - Tạo ứng dụng mới với Redirect URI là `https://yourdomain.com/Account/ExternalLoginCallback`
   - Sao chép Client ID và Client Secret

4. Thiết lập tích hợp tự động giữa tài khoản người dùng:
   - Chạy script tự động đồng bộ người dùng:
   ```powershell
   cd GitIntegration
   .\setup-gitea.ps1
   ```

#### 6. Sử Dụng Gitea trong DevCommunity

1. **Tạo Repository**:
   - Vào mục "Mã Nguồn" trong DevCommunity
   - Nhấn "Tạo Repository" và điền thông tin cần thiết
   - Repository sẽ được tạo đồng thời trên Gitea

2. **Quản Lý Mã Nguồn**:
   - Tạo, chỉnh sửa file trực tiếp qua giao diện web
   - Xem lịch sử commit và thay đổi
   - Sử dụng Git client để clone và push code

3. **Xem và Làm Việc với Repository**:
   - Clone repository về máy:
   ```bash
   git clone http://localhost:3000/username/repository-name.git
   ```
   - Push thay đổi:
   ```bash
   git add .
   git commit -m "Your commit message"
   git push
   ```

#### 7. Khắc Phục Sự Cố Thường Gặp

- **Không kết nối được với Gitea**: Kiểm tra cài đặt URL và cổng trong `appsettings.json`
- **Lỗi xác thực**: Đảm bảo Access Token còn hạn và có đủ quyền
- **Repository không đồng bộ**: Sử dụng công cụ kiểm tra và đồng bộ trong phần quản trị

---

## 🤝 Đóng Góp

Chúng tôi luôn chào đón các đóng góp từ cộng đồng! Nếu bạn muốn tham gia phát triển dự án:

1. Fork repository
2. Tạo branch mới (`git checkout -b feature/your-feature`)
3. Commit thay đổi (`git commit -m 'Add some feature'`)
4. Push lên branch của bạn (`git push origin feature/your-feature`)
5. Tạo Pull Request

---

## 📞 Liên Hệ

- 📧 **Email**: support@devcommunity.com
- 🌐 **Website**: [https://devcommunity.com](https://devcommunity.com)

---

## 📄 Giấy Phép

Dự án được phát triển dưới giấy phép MIT. Xem chi tiết tại [LICENSE](LICENSE).

---

<p align="center">Made with ❤️ by DevCommunity Team</p>
