# Hướng dẫn tích hợp Gitea vào DevCommunity

Tài liệu này hướng dẫn cách cài đặt và cấu hình Gitea để tích hợp vào dự án DevCommunity, cho phép người dùng chia sẻ và đóng góp dự án cá nhân thông qua repositories.

## Cài đặt Gitea

### Cài đặt bằng Docker (Khuyến nghị)

1. **Yêu cầu**:
   - [Docker](https://www.docker.com/products/docker-desktop) đã được cài đặt
   - [Docker Compose](https://docs.docker.com/compose/install/) (tùy chọn nhưng được khuyến nghị)

2. **Tạo thư mục để lưu trữ dữ liệu**:
   ```bash
   mkdir -p /path/to/gitea/data
   ```

3. **Tạo file docker-compose.yml**:
   ```yaml
   version: '3'
   services:
     gitea:
       image: gitea/gitea:1.21
       container_name: gitea
       environment:
         - USER_UID=1000
         - USER_GID=1000
         - GITEA__database__DB_TYPE=sqlite3
         - GITEA__database__PATH=/data/gitea/gitea.db
         - GITEA__server__ROOT_URL=http://localhost:3000/
         - GITEA__server__SSH_DOMAIN=localhost
       restart: always
       volumes:
         - ./data:/data
         - /etc/timezone:/etc/timezone:ro
         - /etc/localtime:/etc/localtime:ro
       ports:
         - "3000:3000"
         - "2222:22"
   ```

4. **Khởi động Gitea**:
   ```bash
   docker-compose up -d
   ```

### Cài đặt trực tiếp trên Windows

1. **Tải Gitea**: Tải xuống bản phát hành mới nhất từ [Gitea Releases](https://dl.gitea.io/gitea/)
2. **Tạo thư mục**:
   ```cmd
   mkdir C:\gitea
   ```
3. **Sao chép file gitea.exe vào thư mục**
4. **Chạy Gitea**:
   ```cmd
   cd C:\gitea
   gitea.exe web
   ```

## Cấu hình Gitea lần đầu

1. **Truy cập giao diện web**:
   Mở trình duyệt và truy cập `http://localhost:3000`

2. **Thiết lập ban đầu**:
   - Loại Database: SQLite3
   - Tên trang: DevCommunity Repositories
   - Đường dẫn gốc: `http://localhost:3000/`
   - Tạo tài khoản quản trị:
     - Tên đăng nhập: admin
     - Email: admin@example.com
     - Mật khẩu: (tạo mật khẩu mạnh)

3. **Đăng nhập vào tài khoản admin**

4. **Cấu hình chỉnh sửa tìm kiếm Elasticsearch (tùy chọn)**:
   Từ trang quản lý, vào `Site Administration > Settings > Search Settings`

## Tạo Admin API Token

1. **Đăng nhập vào Gitea** bằng tài khoản admin
2. **Tạo API token**:
   - Vào `Settings` > `Applications` > `Generate New Token`
   - Đặt tên: `DevCommunity Integration`
   - Chọn phạm vi: Chọn tất cả các quyền
   - Nhấn `Generate Token`
3. **Sao chép token** được tạo ra - token này sẽ được sử dụng trong cấu hình DevCommunity

## Cấu hình DevCommunity để tích hợp với Gitea

1. **Cập nhật file appsettings.json**:
   ```json
   "Gitea": {
     "ApiUrl": "http://localhost:3000/api/v1",
     "AdminToken": "your-gitea-admin-token-here",
     "WebUrl": "http://localhost:3000"
   }
   ```
   Thay `your-gitea-admin-token-here` bằng token đã tạo ở bước trước.

2. **Chạy script SQL để thêm cột Gitea vào bảng Users**:
   ```sql
   -- Thêm các cột gitea_username và gitea_token vào bảng Users
   IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'gitea_username')
   BEGIN
       ALTER TABLE [dbo].[Users]
       ADD [gitea_username] NVARCHAR(50) NULL;
   END

   IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'gitea_token')
   BEGIN
       ALTER TABLE [dbo].[Users]
       ADD [gitea_token] NVARCHAR(255) NULL;
   END
   ```

## Khởi động lại ứng dụng

Sau khi đã cài đặt và cấu hình Gitea, khởi động lại ứng dụng DevCommunity:

```bash
dotnet run
```

## Kiểm tra tích hợp

1. Đăng nhập vào DevCommunity
2. Truy cập trang Repositories
3. Tạo một repository mới
4. Kiểm tra xem repository đã được tạo trong cả DevCommunity và Gitea

## Xử lý sự cố

### Kết nối với Gitea
- Đảm bảo Gitea đang chạy và có thể truy cập được từ DevCommunity
- Kiểm tra xem token API đã được cấu hình đúng trong appsettings.json
- Kiểm tra log của Gitea để xem có lỗi không

### Tạo người dùng trong Gitea
- Đảm bảo tài khoản admin của Gitea có quyền tạo người dùng mới
- Kiểm tra xem email người dùng có hợp lệ không

### Tạo repository
- Đảm bảo người dùng đã được đồng bộ với Gitea
- Kiểm tra log của DevCommunity để xem lỗi chi tiết nếu có

## Tài liệu tham khảo

- [Gitea API Documentation](https://try.gitea.io/api/swagger)
- [Gitea Administrator Guide](https://docs.gitea.io/en-us/administration/)
- [Gitea Configuration Cheat Sheet](https://docs.gitea.io/en-us/config-cheat-sheet/) 