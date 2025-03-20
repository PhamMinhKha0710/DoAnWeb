using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using BCrypt.Net;

namespace DoAnWeb.Services
{
    /// <summary>
    /// Service quản lý việc mã hóa mật khẩu với nhiều thuật toán khác nhau
    /// hỗ trợ chuyển đổi từ thuật toán cũ sang mới an toàn hơn
    /// </summary>
    public interface IPasswordHashService
    {
        /// <summary>
        /// Tạo hash từ mật khẩu sử dụng thuật toán mặc định (BCrypt)
        /// </summary>
        string HashPassword(string password);
        
        /// <summary>
        /// Xác thực mật khẩu dựa trên loại hash được sử dụng
        /// </summary>
        bool VerifyPassword(string password, string hash, string hashType);
        
        /// <summary>
        /// Kiểm tra nếu hash cần được nâng cấp (sử dụng thuật toán cũ)
        /// </summary>
        bool NeedsUpgrade(string hashType);
        
        /// <summary>
        /// Lấy loại hash mặc định hiện tại
        /// </summary>
        string GetDefaultHashType();
    }
    
    public class PasswordHashService : IPasswordHashService
    {
        // Các loại hash được hỗ trợ
        public const string HASH_TYPE_SHA256 = "SHA256";
        public const string HASH_TYPE_PBKDF2 = "PBKDF2";
        public const string HASH_TYPE_BCRYPT = "BCRYPT";
        
        // Số vòng lặp đối với PBKDF2 (tăng số này sẽ làm cho việc băm mật khẩu chậm hơn)
        private const int PBKDF2_ITERATIONS = 10000;

        // WorkFactor cho BCrypt (giá trị từ 10-16 là phổ biến, 12 là khuyến nghị)
        private const int BCRYPT_WORK_FACTOR = 12;
        
        /// <summary>
        /// Băm mật khẩu sử dụng BCrypt với salt tự động
        /// </summary>
        public string HashPassword(string password)
        {
            // Sử dụng BCrypt để hash mật khẩu (tự động tạo và nhúng salt)
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: BCRYPT_WORK_FACTOR);
        }
        
        /// <summary>
        /// Xác thực mật khẩu dựa trên loại hash
        /// </summary>
        public bool VerifyPassword(string password, string storedHash, string hashType)
        {
            switch (hashType)
            {
                case HASH_TYPE_SHA256:
                    return VerifyPasswordSHA256(password, storedHash);
                    
                case HASH_TYPE_PBKDF2:
                    return VerifyPasswordPBKDF2(password, storedHash);
                    
                case HASH_TYPE_BCRYPT:
                    return VerifyPasswordBCrypt(password, storedHash);
                    
                default:
                    // Mặc định sử dụng SHA256 cho các tài khoản cũ
                    return VerifyPasswordSHA256(password, storedHash);
            }
        }
        
        /// <summary>
        /// Xác thực mật khẩu với hash SHA256 đơn giản (không salt)
        /// </summary>
        private bool VerifyPasswordSHA256(string password, string storedHash)
        {
            string computedHash;
            
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                computedHash = Convert.ToBase64String(hashedBytes);
            }
            
            return computedHash == storedHash;
        }
        
        /// <summary>
        /// Xác thực mật khẩu với PBKDF2 (bao gồm salt)
        /// </summary>
        private bool VerifyPasswordPBKDF2(string password, string storedHash)
        {
            try
            {
                // Giải mã chuỗi Base64 thành mảng byte
                byte[] storedBytes = Convert.FromBase64String(storedHash);
                
                // Kiểm tra xem storedBytes có đủ dài không
                if (storedBytes.Length < 16 + 32)
                {
                    return false;
                }
                
                // Tách salt và hash
                byte[] salt = new byte[16];
                byte[] hash = new byte[32];
                Array.Copy(storedBytes, 0, salt, 0, 16);
                Array.Copy(storedBytes, 16, hash, 0, 32);
                
                // Tính toán hash mới với salt đã lưu
                byte[] computedHash = KeyDerivation.Pbkdf2(
                    password: password,
                    salt: salt,
                    prf: KeyDerivationPrf.HMACSHA256,
                    iterationCount: PBKDF2_ITERATIONS,
                    numBytesRequested: 32);
                
                // So sánh hash tính toán với hash đã lưu
                return SlowEquals(hash, computedHash);
            }
            catch
            {
                // Nếu có bất kỳ lỗi nào xảy ra trong quá trình xác thực
                return false;
            }
        }

        /// <summary>
        /// Xác thực mật khẩu với BCrypt
        /// </summary>
        private bool VerifyPasswordBCrypt(string password, string storedHash)
        {
            try
            {
                // BCrypt tự xử lý việc trích xuất salt và tính toán hash
                return BCrypt.Net.BCrypt.Verify(password, storedHash);
            }
            catch
            {
                // Nếu có bất kỳ lỗi nào xảy ra trong quá trình xác thực
                return false;
            }
        }
        
        /// <summary>
        /// So sánh hai mảng byte theo thời gian không đổi
        /// để tránh các cuộc tấn công timing
        /// </summary>
        private bool SlowEquals(byte[] a, byte[] b)
        {
            uint diff = (uint)a.Length ^ (uint)b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++)
            {
                diff |= (uint)(a[i] ^ b[i]);
            }
            return diff == 0;
        }
        
        /// <summary>
        /// Kiểm tra xem hash có cần được nâng cấp không
        /// </summary>
        public bool NeedsUpgrade(string hashType)
        {
            // Nếu không phải BCRYPT, cần nâng cấp
            return hashType != HASH_TYPE_BCRYPT;
        }
        
        /// <summary>
        /// Lấy loại hash mặc định hiện tại
        /// </summary>
        public string GetDefaultHashType()
        {
            return HASH_TYPE_BCRYPT;
        }
    }
} 