using DoAnWeb.Models;
using DoAnWeb.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DoAnWeb.Services
{
    public class ExternalLoginService : IExternalLoginService
    {
        private readonly DevCommunityContext _context;
        private readonly IUserService _userService;
        private readonly IRepository<ExternalLogin> _externalLoginRepository;

        public ExternalLoginService(
            DevCommunityContext context,
            IUserService userService,
            IRepository<ExternalLogin> externalLoginRepository)
        {
            _context = context;
            _userService = userService;
            _externalLoginRepository = externalLoginRepository;
        }

        public User FindUserByExternalLogin(string provider, string providerKey)
        {
            var externalLogin = _context.ExternalLogins
                .Include(el => el.User)
                .FirstOrDefault(el => el.Provider == provider && el.ProviderKey == providerKey);

            return externalLogin?.User;
        }

        public bool AddExternalLogin(int userId, string provider, string providerKey, string providerDisplayName)
        {
            try
            {
                // Kiểm tra xem đã tồn tại chưa
                var existingLogin = _context.ExternalLogins
                    .FirstOrDefault(el => el.UserId == userId && el.Provider == provider);

                if (existingLogin != null)
                {
                    // Cập nhật thông tin nếu đã tồn tại
                    existingLogin.ProviderKey = providerKey;
                    existingLogin.ProviderDisplayName = providerDisplayName;
                    _context.SaveChanges();
                    return true;
                }

                // Tạo mới nếu chưa tồn tại
                var externalLogin = new ExternalLogin
                {
                    UserId = userId,
                    Provider = provider,
                    ProviderKey = providerKey,
                    ProviderDisplayName = providerDisplayName,
                    CreatedDate = DateTime.Now
                };

                _externalLoginRepository.Add(externalLogin);
                _externalLoginRepository.Save();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool RemoveExternalLogin(int userId, string provider)
        {
            try
            {
                var externalLogin = _context.ExternalLogins
                    .FirstOrDefault(el => el.UserId == userId && el.Provider == provider);

                if (externalLogin == null)
                    return false;

                _context.ExternalLogins.Remove(externalLogin);
                _context.SaveChanges();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public IEnumerable<ExternalLogin> GetExternalLogins(int userId)
        {
            return _context.ExternalLogins
                .Where(el => el.UserId == userId)
                .ToList();
        }

        public async Task<User> ProcessExternalLoginAsync(ClaimsPrincipal principal, string provider)
        {
            // Lấy thông tin từ claims
            var nameIdentifier = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = principal.FindFirstValue(ClaimTypes.Email);
            var name = principal.FindFirstValue(ClaimTypes.Name);
            
            if (string.IsNullOrEmpty(nameIdentifier))
                return null;

            // Tạo giá trị mặc định cho email nếu không có
            if (string.IsNullOrEmpty(email))
            {
                email = $"{nameIdentifier}@{provider.ToLower()}.account";
            }

            // Tạo username từ email nếu không có name
            var username = name ?? email.Split('@')[0];
            
            // Kiểm tra xem đã có tài khoản với external login này chưa
            var user = FindUserByExternalLogin(provider, nameIdentifier);

            if (user != null)
            {
                // Cập nhật thông tin đăng nhập
                user.LastLoginDate = DateTime.Now;
                _userService.UpdateUser(user);
                return user;
            }

            // Kiểm tra xem đã có tài khoản với email này chưa
            var existingUser = _userService.GetUserByEmail(email);

            if (existingUser != null)
            {
                // Liên kết tài khoản hiện có với external login
                AddExternalLogin(existingUser.UserId, provider, nameIdentifier, provider);
                
                // Cập nhật thông tin đăng nhập
                existingUser.LastLoginDate = DateTime.Now;
                _userService.UpdateUser(existingUser);
                
                return existingUser;
            }

            // Tạo tài khoản mới nếu chưa tồn tại
            var newUser = new User
            {
                Email = email,
                Username = GenerateUniqueUsername(username),
                DisplayName = name ?? username,
                IsEmailVerified = true // Coi như đã xác thực qua provider
            };

            // Tạo mật khẩu ngẫu nhiên
            string password = Guid.NewGuid().ToString();
            _userService.CreateUser(newUser, password);

            // Liên kết tài khoản mới với external login
            AddExternalLogin(newUser.UserId, provider, nameIdentifier, provider);

            return newUser;
        }

        private string GenerateUniqueUsername(string baseUsername)
        {
            // Xóa các ký tự không hợp lệ từ username
            var username = new string(baseUsername.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
            
            // Đảm bảo username không bắt đầu bằng số
            if (username.Length > 0 && char.IsDigit(username[0]))
            {
                username = "user_" + username;
            }

            // Thêm provider vào cuối tên để giảm khả năng trùng lặp
            var finalUsername = username;
            int count = 0;

            // Kiểm tra và tạo username không trùng lặp
            while (_userService.UsernameExists(finalUsername))
            {
                count++;
                finalUsername = $"{username}_{count}";
            }

            return finalUsername;
        }
    }
} 