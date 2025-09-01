namespace FinancialMonitoring.Models;

/// <summary>
/// Repository interface for managing authenticated users
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Retrieves a user by their username
    /// </summary>
    /// <param name="username">The username to search for</param>
    /// <returns>The user if found, null otherwise</returns>
    Task<AuthUser?> GetByUsernameAsync(string username);

    /// <summary>
    /// Retrieves a user by their email address
    /// </summary>
    /// <param name="email">The email to search for</param>
    /// <returns>The user if found, null otherwise</returns>
    Task<AuthUser?> GetByEmailAsync(string email);

    /// <summary>
    /// Retrieves a user by their ID
    /// </summary>
    /// <param name="id">The user ID</param>
    /// <returns>The user if found, null otherwise</returns>
    Task<AuthUser?> GetByIdAsync(int id);

    /// <summary>
    /// Retrieves all users in the system
    /// </summary>
    /// <returns>Collection of all users</returns>
    Task<IEnumerable<AuthUser>> GetAllAsync();

    /// <summary>
    /// Creates a new user
    /// </summary>
    /// <param name="user">The user to create</param>
    /// <returns>The created user with assigned ID</returns>
    Task<AuthUser> CreateAsync(AuthUser user);

    /// <summary>
    /// Updates an existing user
    /// </summary>
    /// <param name="user">The user to update</param>
    /// <returns>The updated user</returns>
    Task<AuthUser> UpdateAsync(AuthUser user);

    /// <summary>
    /// Updates the last login timestamp for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>Task representing the async operation</returns>
    Task UpdateLastLoginAsync(int userId);
}