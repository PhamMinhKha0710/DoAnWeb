using DoAnWeb.Models;
using DoAnWeb.Repositories;
using System.Security.Cryptography;
using System.Text;

namespace DoAnWeb.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
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

            // Hash password
            user.PasswordHash = HashPassword(password);

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

            // Check if password is correct
            if (!VerifyPassword(password, user.PasswordHash))
                return null;

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

        // Helper methods for password hashing
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private bool VerifyPassword(string password, string hash)
        {
            var passwordHash = HashPassword(password);
            return passwordHash == hash;
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
                
            // Verify current password
            if (!VerifyPassword(currentPassword, user.PasswordHash))
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
            if (VerifyPassword(newPassword, user.PasswordHash))
                throw new ArgumentException("New password must be different from your current password.");
            
            // Update password
            user.PasswordHash = HashPassword(newPassword);
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
                
            // Verify password
            if (!VerifyPassword(password, user.PasswordHash))
                return false;
            
            // Delete user
            _userRepository.Delete(userId);
            _userRepository.Save();
            
            return true;
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
    }
}