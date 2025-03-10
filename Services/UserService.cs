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

            // Hash password
            user.PasswordHash = HashPassword(password);

            // Set creation date
            user.CreatedDate = DateTime.Now;
            user.UpdatedDate = DateTime.Now;

            // Create user
            _userRepository.Add(user);
            _userRepository.Save();
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
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            string passwordHash = HashPassword(password);
            return passwordHash.Equals(storedHash);
        }
    }
}