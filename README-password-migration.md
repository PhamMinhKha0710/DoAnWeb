# Hướng dẫn nâng cấp hệ thống xác thực mật khẩu

Tài liệu này mô tả các bước đã thực hiện để nâng cấp hệ thống xác thực mật khẩu từ thuật toán SHA-256 đơn giản sang thuật toán PBKDF2 an toàn hơn, đồng thời bảo đảm khả năng đăng nhập tương thích với các tài khoản cũ.

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
   - PBKDF2: Dùng thuật toán PBKDF2 với salt (cho tài khoản mới)
4. Nếu đăng nhập thành công và thuật toán băm là loại cũ (SHA256), tự động nâng cấp mật khẩu sang thuật toán PBKDF2 an toàn hơn

### Đăng ký tài khoản mới

1. Tất cả tài khoản mới được tạo sẽ sử dụng thuật toán PBKDF2 với salt
2. Trường `HashType` sẽ được đặt thành "PBKDF2"

## Cách triển khai

1. Chạy script SQL để thêm cột HashType vào bảng Users:
   ```
   -- Add HashType column to Users table
   ALTER TABLE Users ADD
       HashType NVARCHAR(50) NOT NULL DEFAULT 'SHA256';
   ```

2. Khởi động lại ứng dụng để áp dụng các thay đổi code

## Phương thức băm mật khẩu mới

Thuật toán PBKDF2 được triển khai với các đặc điểm sau:
- Sử dụng thuật toán HMACSHA256
- 10.000 vòng lặp để làm chậm các cuộc tấn công bằng vét cạn
- Salt ngẫu nhiên 16 byte cho mỗi mật khẩu
- Chiều dài hash 32 byte
- Salt và hash được lưu cùng nhau trong một chuỗi Base64

## Lưu ý về bảo mật

- Thuật toán SHA-256 đơn giản (không salt) vẫn được hỗ trợ cho các tài khoản cũ
- Khi người dùng đăng nhập thành công, hệ thống sẽ tự động nâng cấp mật khẩu sang định dạng mới
- Nên khuyến khích người dùng đổi mật khẩu nếu họ chưa đăng nhập sau khi nâng cấp 