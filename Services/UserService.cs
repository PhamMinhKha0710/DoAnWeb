using DoAnWeb.Models;
using DoAnWeb.Repositories;
using DoAnWeb.ViewModels;
using System.Security.Cryptography;
using System.Text;

namespace DoAnWeb.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPostRepository? _postRepository;
        private readonly ICommentRepository _commentRepository;
        private readonly IPasswordHashService _passwordHashService;

        public UserService(
            IUserRepository userRepository, 
            ICommentRepository commentRepository, 
            IPasswordHashService passwordHashService,
            IPostRepository? postRepository = null)
        {
            _userRepository = userRepository;
            _postRepository = postRepository;
            _commentRepository = commentRepository;
            _passwordHashService = passwordHashService;
        }

        public IEnumerable<User> GetAllUsers()
        {
            return _userRepository.GetAll();
        }

        public User GetUserById(int id)
        {
            return _userRepository.GetById(id);
        }

        public User GetUserByUsername(string username)
        {
            return _userRepository.GetByUsername(username);
        }

        public User GetUserByEmail(string email)
        {
            return _userRepository.GetByEmail(email);
        }

        public void CreateExternalUser(User user)
        {
            // Validate user data
            if (string.IsNullOrEmpty(user.Email))
                throw new ArgumentException("Email is required");

            if (string.IsNullOrEmpty(user.Username))
                throw new ArgumentException("Username is required");

            if (_userRepository.UsernameExists(user.Username))
                throw new ArgumentException("Username already exists");

            if (_userRepository.EmailExists(user.Email))
                throw new ArgumentException("Email already exists");

            // Set user properties for external login
            user.CreatedDate = DateTime.Now;
            user.UpdatedDate = DateTime.Now;
            user.IsEmailVerified = true; // Email is verified through external provider
            user.HashType = "EXTERNAL"; // Mark that this is an external login

            // Add the user
            _userRepository.Add(user);
            _userRepository.Save();
        }

        public void CreateUser(User user, string password)
        {
            // Validate user data
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password is required");

            if (_userRepository.UsernameExists(user.Username))
                throw new ArgumentException("Username already exists");

            if (_userRepository.EmailExists(user.Email))
                throw new ArgumentException("Email already exists");
        
            // Validate password complexity
            if (!IsPasswordComplex(password))
                throw new ArgumentException("Password does not meet complexity requirements. It must be at least 8 characters long and include uppercase letters, lowercase letters, numbers, and special characters.");

            // Hash password using the new service
            user.PasswordHash = _passwordHashService.HashPassword(password);
            user.HashType = _passwordHashService.GetDefaultHashType();

            // Set creation date
            user.CreatedDate = DateTime.Now;
            user.UpdatedDate = DateTime.Now;
            user.LastPasswordChangeDate = DateTime.Now;
            
            // Set up email verification
            user.IsEmailVerified = false;
            user.VerificationToken = GenerateEmailVerificationToken(0); // We'll update this with the correct user ID after creation
            user.VerificationTokenExpiry = DateTime.Now.AddDays(7); // Token valid for 7 days

            // Create user
            _userRepository.Add(user);
            _userRepository.Save();
            
            // TODO: Send verification email to the user's email address
            // For now, we'll just assume the email is sent
        }

        public void UpdateUser(User user)
        {
            // Get existing user
            var existingUser = _userRepository.GetById(user.UserId);
            if (existingUser == null)
                throw new ArgumentException("User not found");

            // Check if username is changed and already exists
            if (user.Username != existingUser.Username && _userRepository.UsernameExists(user.Username))
                throw new ArgumentException("Username already exists");

            // Check if email is changed and already exists
            if (user.Email != existingUser.Email && _userRepository.EmailExists(user.Email))
                throw new ArgumentException("Email already exists");

            // Update user properties
            existingUser.Username = user.Username;
            existingUser.Email = user.Email;
            existingUser.DisplayName = user.DisplayName;
            existingUser.AvatarUrl = user.AvatarUrl;
            existingUser.Bio = user.Bio;
            existingUser.UpdatedDate = DateTime.Now;

            // Update user
            _userRepository.Update(existingUser);
            _userRepository.Save();
        }

        public void DeleteUser(int id)
        {
            _userRepository.Delete(id);
            _userRepository.Save();
        }

        public User Authenticate(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return null;

            var user = _userRepository.GetByUsername(username);

            // Check if user exists
            if (user == null)
                return null;

            // Check if password is correct using appropriate hash algorithm
            if (!_passwordHashService.VerifyPassword(password, user.PasswordHash, user.HashType))
                return null;

            // Check if hash algorithm needs upgrade
            if (_passwordHashService.NeedsUpgrade(user.HashType))
            {
                // Nâng cấp hash mật khẩu sang phương thức mới an toàn hơn
                user.PasswordHash = _passwordHashService.HashPassword(password);
                user.HashType = _passwordHashService.GetDefaultHashType();
                user.UpdatedDate = DateTime.Now;
                user.LastPasswordChangeDate = DateTime.Now;
                
                // Cập nhật người dùng
                _userRepository.Update(user);
                _userRepository.Save();
            }

            // Authentication successful
            return user;
        }

        public bool UsernameExists(string username)
        {
            return _userRepository.UsernameExists(username);
        }

        public bool EmailExists(string email)
        {
            return _userRepository.EmailExists(email);
        }

        // Helper method to check password complexity
        private bool IsPasswordComplex(string password)
        {
            // At least 8 characters
            if (password.Length < 8)
                return false;
                
            // At least one uppercase letter
            if (!password.Any(char.IsUpper))
                return false;
                
            // At least one lowercase letter
            if (!password.Any(char.IsLower))
                return false;
                
            // At least one digit
            if (!password.Any(char.IsDigit))
                return false;
                
            // At least one special character
            if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
                return false;
                
            return true;
        }

        public bool ChangePassword(int userId, string currentPassword, string newPassword)
        {
            // Validate input
            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword))
                return false;
                
            // Get user
            var user = _userRepository.GetById(userId);
            if (user == null)
                return false;
                
            // Verify current password using appropriate hash algorithm
            if (!_passwordHashService.VerifyPassword(currentPassword, user.PasswordHash, user.HashType))
                return false;
            
            // Account verification check - Ensure the user's email is verified
            if (!user.IsEmailVerified)
                throw new InvalidOperationException("You must verify your email address before changing your password. Please check your email for a verification link.");
            
            // Additional security check - Ensure the account is not locked
            if (user.IsLocked)
                throw new InvalidOperationException("Your account is currently locked. Please contact support to unlock your account before changing your password.");
            
            // Password complexity validation
            if (!IsPasswordComplex(newPassword))
                throw new ArgumentException("New password does not meet security requirements. Please ensure it contains at least 8 characters, including uppercase, lowercase, numbers, and special characters.");
            
            // Ensure new password is different from the current one
            if (_passwordHashService.VerifyPassword(newPassword, user.PasswordHash, user.HashType))
                throw new ArgumentException("New password must be different from your current password.");
            
            // Update password with modern hash
            user.PasswordHash = _passwordHashService.HashPassword(newPassword);
            user.HashType = _passwordHashService.GetDefaultHashType();
            user.UpdatedDate = DateTime.Now;
            user.LastPasswordChangeDate = DateTime.Now;
            
            // Record password change in security log (if you have such functionality)
            // LogSecurityEvent(userId, "PasswordChanged", "User changed their password");
            
            _userRepository.Update(user);
            _userRepository.Save();
            
            return true;
        }
        
        public bool DeleteAccount(int userId, string password)
        {
            // Validate input
            if (string.IsNullOrEmpty(password))
                return false;
                
            // Get user
            var user = _userRepository.GetById(userId);
            if (user == null)
                return false;
                
            // Verify password using appropriate hash algorithm
            if (!_passwordHashService.VerifyPassword(password, user.PasswordHash, user.HashType))
                return false;
            
            // Delete user
            _userRepository.Delete(userId);
            _userRepository.Save();
            
            return true;
        }

        public bool VerifyEmail(string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;
                
            // Find user with matching verification token
            var user = _userRepository.GetAll().FirstOrDefault(u => u.VerificationToken == token);
            if (user == null)
                return false;
                
            // Check if token has expired
            if (user.VerificationTokenExpiry.HasValue && user.VerificationTokenExpiry.Value < DateTime.Now)
                return false;
                
            // Mark email as verified
            user.IsEmailVerified = true;
            user.VerificationToken = null;
            user.VerificationTokenExpiry = null;
            user.UpdatedDate = DateTime.Now;
            
            // Update user
            _userRepository.Update(user);
            _userRepository.Save();
            
            return true;
        }

        public bool SendVerificationEmail(int userId)
        {
            // Get user
            var user = _userRepository.GetById(userId);
            if (user == null)
                return false;
                
            // Check if email is already verified
            if (user.IsEmailVerified)
                return false;
                
            // Generate token
            string token = GenerateEmailVerificationToken(userId);
            
            // Save token and expiry to user record
            user.VerificationToken = token;
            user.VerificationTokenExpiry = DateTime.Now.AddDays(7); // Token valid for 7 days
            
            // Update user
            _userRepository.Update(user);
            _userRepository.Save();
            
            // Here you would send the actual email with the verification token
            // For this implementation, we'll just return true as if the email was sent
            
            // The verification link would be something like:
            // https://yourdomain.com/Account/VerifyEmail?token={token}
            
            // TODO: Implement email sending functionality
            
            return true;
        }

        public string GenerateEmailVerificationToken(int userId)
        {
            // Generate a unique token
            byte[] tokenData = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenData);
            }
            
            // Convert to base64 string and remove any non-alphanumeric characters
            string token = Convert.ToBase64String(tokenData)
                .Replace("/", "_")
                .Replace("+", "-")
                .Replace("=", "");
            
            return token;
        }

        public ProfileViewModel GetUserProfile(int userId)
        {
            var user = _userRepository.GetById(userId);
            if (user == null)
            {
                return null;
            }

            // Tạm thời trả về 0 cho postCount - lớp Post chưa được đăng ký trong DbContext
            // var postCount = _postRepository.GetPostCountByUserId(userId);
            var postCount = 0; // Không sử dụng PostRepository vì chưa có bảng Posts trong database
            var commentCount = _commentRepository.GetCommentCountByUserId(userId);
            var tagCount = 0; // Implement if you have a way to count tags created by user

            return new ProfileViewModel
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                DisplayName = user.DisplayName,
                Bio = user.Bio ?? string.Empty,
                AvatarUrl = user.AvatarUrl ?? string.Empty,
                PostCount = postCount,
                CommentCount = commentCount,
                TagCount = tagCount,
                Reputation = user.Reputation,
                MemberSince = user.CreatedDate,
                // Add Gitea information and last login date
                GiteaUsername = user.GiteaUsername,
                LastLoginDate = user.LastLoginDate
            };
        }

        public string GeneratePasswordResetToken(string email)
        {
            // Get user by email
            var user = _userRepository.GetByEmail(email);
            if (user == null)
                return null;
            
            // Generate a unique token
            byte[] tokenData = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenData);
            }
            
            // Convert to base64 string and remove any non-alphanumeric characters
            string token = Convert.ToBase64String(tokenData)
                .Replace("/", "_")
                .Replace("+", "-")
                .Replace("=", "");
            
            // Save token and expiry to user record
            user.PasswordResetToken = token;
            user.PasswordResetTokenExpiry = DateTime.Now.AddHours(24); // Token valid for 24 hours
            
            // Update user
            _userRepository.Update(user);
            _userRepository.Save();
            
            return token;
        }

        public bool ValidatePasswordResetToken(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
                return false;
            
            // Get user by email
            var user = _userRepository.GetByEmail(email);
            if (user == null || user.PasswordResetToken != token)
                return false;
            
            // Check if token has expired
            if (!user.PasswordResetTokenExpiry.HasValue || user.PasswordResetTokenExpiry.Value < DateTime.Now)
                return false;
            
            return true;
        }

        public bool ResetPassword(string email, string token, string newPassword)
        {
            // Validate token
            if (!ValidatePasswordResetToken(email, token))
                return false;
            
            // Validate password complexity
            if (!IsPasswordComplex(newPassword))
                return false;
            
            // Get user
            var user = _userRepository.GetByEmail(email);
            
            // Update password with modern hash
            user.PasswordHash = _passwordHashService.HashPassword(newPassword);
            user.HashType = _passwordHashService.GetDefaultHashType();
            user.UpdatedDate = DateTime.Now;
            user.LastPasswordChangeDate = DateTime.Now;
            
            // Clear reset token
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;
            
            // Update user
            _userRepository.Update(user);
            _userRepository.Save();
            
            return true;
        }
    }
}