using DoAnWeb.Models;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DoAnWeb.Services
{
    public interface IExternalLoginService
    {
        /// <summary>
        /// Finds a user by their external login info
        /// </summary>
        /// <param name="provider">The name of the provider (e.g., "Google", "GitHub")</param>
        /// <param name="providerKey">The unique key from the provider</param>
        /// <returns>The user if found, null otherwise</returns>
        User FindUserByExternalLogin(string provider, string providerKey);
        
        /// <summary>
        /// Creates a new external login for a user
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <param name="provider">The name of the provider</param>
        /// <param name="providerKey">The unique key from the provider</param>
        /// <param name="providerDisplayName">The display name for the provider</param>
        /// <returns>True if successful, false otherwise</returns>
        bool AddExternalLogin(int userId, string provider, string providerKey, string providerDisplayName);
        
        /// <summary>
        /// Removes an external login
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <param name="provider">The name of the provider</param>
        /// <returns>True if successful, false otherwise</returns>
        bool RemoveExternalLogin(int userId, string provider);
        
        /// <summary>
        /// Gets the list of external logins for a user
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <returns>A list of external logins</returns>
        IEnumerable<ExternalLogin> GetExternalLogins(int userId);
        
        /// <summary>
        /// Process the claims from an external login and returns a user
        /// Either finds an existing user or creates a new one
        /// </summary>
        /// <param name="claims">The claims from the external provider</param>
        /// <param name="provider">The name of the provider</param>
        /// <returns>The user associated with the external login</returns>
        Task<User> ProcessExternalLoginAsync(ClaimsPrincipal claims, string provider);
    }
} 