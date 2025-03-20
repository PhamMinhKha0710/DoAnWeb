# Hướng dẫn nâng cấp hệ thống xác thực mật khẩu

Tài liệu này mô tả các bước đã thực hiện để nâng cấp hệ thống xác thực mật khẩu từ thuật toán SHA-256 đơn giản sang thuật toán BCrypt an toàn hơn, đồng thời bảo đảm khả năng đăng nhập tương thích với các tài khoản cũ.

## Tổng quan về các thay đổi

1. Thêm cột `HashType` vào bảng `Users` để theo dõi loại thuật toán băm được sử dụng cho mỗi tài khoản
2. Tạo service `PasswordHashService` mới để quản lý nhiều phương thức băm mật khẩu
3. Cập nhật `UserService` để sử dụng service mới và tự động nâng cấp mật khẩu cũ sang định dạng mới khi người dùng đăng nhập thành công

## Các tệp đã thay đổi

1. **add-hash-type-column.sql**: Script SQL để thêm cột HashType vào bảng Users
2. **Models/User.cs**: Thêm thuộc tính HashType vào model User
3. **Services/PasswordHashService.cs**: Service mới để xử lý các phương thức băm mật khẩu khác nhau
4. **Services/UserService.cs**: Cập nhật để sử dụng PasswordHashService thay vì các phương thức băm cũ
5. **Program.cs**: Đăng ký PasswordHashService vào container DI

## Luồng hoạt động mới

### Đăng nhập

1. Người dùng nhập username và password
2. `UserService.Authenticate` tìm người dùng theo username
3. Xác thực mật khẩu dựa trên giá trị của trường `HashType`:
   - SHA256: Dùng thuật toán SHA-256 đơn giản (cho tài khoản cũ)
   - PBKDF2: Dùng thuật toán PBKDF2 với salt (cho tài khoản cũ hơn)
   - BCRYPT: Dùng thuật toán BCrypt với salt tích hợp (cho tài khoản mới)
4. Nếu đăng nhập thành công và thuật toán băm là loại cũ (SHA256 hoặc PBKDF2), tự động nâng cấp mật khẩu sang thuật toán BCrypt an toàn hơn

### Đăng ký tài khoản mới

1. Tất cả tài khoản mới được tạo sẽ sử dụng thuật toán BCrypt với salt tích hợp
2. Trường `HashType` sẽ được đặt thành "BCRYPT"

## Cách triển khai

1. Chạy script SQL để thêm cột HashType vào bảng Users:
   ```
   -- Add HashType column to Users table
   ALTER TABLE Users ADD
       HashType NVARCHAR(50) NOT NULL DEFAULT 'SHA256';
   ```

2. Cài đặt thư viện BCrypt.NET:
   ```
   dotnet add package BCrypt.Net-Next
   ```

3. Khởi động lại ứng dụng để áp dụng các thay đổi code

## Phương thức băm mật khẩu mới

Thuật toán BCrypt được triển khai với các đặc điểm sau:
- Tự động tạo và quản lý salt cho mỗi mật khẩu
- WorkFactor 12 (có thể điều chỉnh để cân bằng giữa bảo mật và hiệu suất)
- Chống lại các tấn công brute-force và rainbow table
- Tự điều chỉnh theo sự phát triển của phần cứng (qua việc điều chỉnh WorkFactor)
- Salt được nhúng trực tiếp trong hash, không cần lưu trữ riêng

## Lợi ích của BCrypt so với PBKDF2 và SHA-256

- **Thiết kế đặc biệt cho mật khẩu**: BCrypt được tạo ra với mục đích chuyên biệt để hash mật khẩu
- **Chống tấn công GPU**: BCrypt yêu cầu nhiều bộ nhớ, làm giảm hiệu quả của tấn công bằng GPU
- **Tham số điều chỉnh (WorkFactor)**: Dễ dàng điều chỉnh độ phức tạp theo thời gian
- **Tích hợp salt**: Tự động xử lý việc tạo và lưu trữ salt, giảm rủi ro lỗi triển khai

## Lưu ý về bảo mật

- Thuật toán SHA-256 đơn giản (không salt) và PBKDF2 vẫn được hỗ trợ cho các tài khoản cũ
- Khi người dùng đăng nhập thành công, hệ thống sẽ tự động nâng cấp mật khẩu sang định dạng BCrypt mới
- Nên khuyến khích người dùng đổi mật khẩu nếu họ chưa đăng nhập sau khi nâng cấp 