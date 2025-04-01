# 🚀 DevCommunity

## 💻 Cộng đồng chia sẻ mã nguồn & hỏi đáp lập trình

![DevCommunity Logo](https://github.com/user-attachments/assets/4d958968-526b-446a-9379-4e0231cd9ee8)

DevCommunity là nền tảng kết nối lập trình viên thông qua việc chia sẻ mã nguồn và hỏi đáp kỹ thuật. Dự án được xây dựng trên nền tảng ASP.NET Core MVC, kết hợp các tính năng của GitHub và Stack Overflow để tạo môi trường học tập và chia sẻ kiến thức hiệu quả.

---

## ✨ Tính Năng Đã Triển Khai

### 👥 Quản Lý Người Dùng
- **Đăng ký** & **đăng nhập** với ASP.NET Identity
- Quản lý hồ sơ cá nhân với avatar, thông tin liên hệ
- Theo dõi hoạt động và đóng góp của người dùng

### ❓ Hệ Thống Hỏi Đáp
- Đăng câu hỏi với trình soạn thảo Markdown đầy đủ
- Hỗ trợ định dạng phong phú với trình soạn thảo trực quan
- Chức năng xem trước (Preview) nội dung Markdown
- Đính kèm tập tin đa dạng (hình ảnh, PDF, tài liệu, mã nguồn)
- Hệ thống tag để phân loại câu hỏi

### 📦 Quản Lý Mã Nguồn
- Tạo và quản lý repository công khai/riêng tư
- Quản lý file và thư mục với giao diện trực quan
- Hỗ trợ tạo file mới với nhiều template có sẵn
- Tích hợp với Gitea cho quản lý mã nguồn

### 🔍 Tìm Kiếm & Phân Loại
- Hệ thống tag linh hoạt cho câu hỏi và repository
- Tìm kiếm nội dung theo nhiều tiêu chí
- Phân loại câu hỏi và mã nguồn theo danh mục

---

## 🛠️ Công Nghệ Sử Dụng

### 🖥️ Backend
- **ASP.NET Core MVC 6.0**: Nền tảng phát triển ứng dụng web
- **Entity Framework Core**: ORM để tương tác với cơ sở dữ liệu
- **ASP.NET Identity**: Quản lý xác thực và phân quyền người dùng
- **Gitea API Integration**: Tích hợp với Gitea cho quản lý mã nguồn

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

- **Hệ thống bình chọn**: Upvote/Downvote cho câu hỏi và câu trả lời
- **Tích hợp thời gian thực**: Sử dụng SignalR cho thông báo tức thì
- **Hệ thống thông báo**: Email và thông báo trong ứng dụng
- **Hệ thống điểm uy tín**: Dựa trên đóng góp và hoạt động của người dùng
- **Chức năng bình luận nâng cao**: Threading và Markdown trong bình luận
- **API công khai**: Cho phép tích hợp với các dịch vụ bên thứ ba

---

## 🚀 Cài Đặt và Chạy Dự Án

### Yêu Cầu Hệ Thống
- .NET 8.0 SDK trở lên
- SQL Server 2019 trở lên
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
