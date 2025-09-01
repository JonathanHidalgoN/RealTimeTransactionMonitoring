using System.Security.Cryptography;
using System.Text;
using FinancialMonitoring.Abstractions;

namespace FinancialMonitoring.Models;

/// <summary>
/// In-memory implementation of user repository for development and testing
/// </summary>
public class InMemoryUserRepository : IUserRepository
{
    private readonly List<AuthUser> _users;
    private int _nextId = 4;

    public InMemoryUserRepository()
    {
        _users = new List<AuthUser>
        {
            new()
            {
                Id = 1,
                Username = "admin",
                Email = "admin@financialmonitoring.com",
                PasswordHash = HashPassword("Admin123!"),
                Role = AuthUserRole.Admin,
                FirstName = "System",
                LastName = "Administrator",
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                IsActive = true
            },
            new()
            {
                Id = 2,
                Username = "analyst",
                Email = "analyst@financialmonitoring.com",
                PasswordHash = HashPassword("Analyst123!"),
                Role = AuthUserRole.Analyst,
                FirstName = "Financial",
                LastName = "Analyst",
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                IsActive = true
            },
            new()
            {
                Id = 3,
                Username = "viewer",
                Email = "viewer@financialmonitoring.com",
                PasswordHash = HashPassword("Viewer123!"),
                Role = AuthUserRole.Viewer,
                FirstName = "Report",
                LastName = "Viewer",
                CreatedAt = DateTime.UtcNow.AddDays(-7),
                IsActive = true
            }
        };
    }

    public Task<AuthUser?> GetByUsernameAsync(string username)
    {
        var user = _users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(user);
    }

    public Task<AuthUser?> GetByEmailAsync(string email)
    {
        var user = _users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(user);
    }

    public Task<AuthUser?> GetByIdAsync(int id)
    {
        var user = _users.FirstOrDefault(u => u.Id == id);
        return Task.FromResult(user);
    }

    public Task<IEnumerable<AuthUser>> GetAllAsync()
    {
        return Task.FromResult(_users.AsEnumerable());
    }

    public Task<AuthUser> CreateAsync(AuthUser user)
    {
        user.Id = _nextId++;
        user.CreatedAt = DateTime.UtcNow;
        _users.Add(user);
        return Task.FromResult(user);
    }

    public Task<AuthUser> UpdateAsync(AuthUser user)
    {
        var existingUser = _users.FirstOrDefault(u => u.Id == user.Id);
        if (existingUser != null)
        {
            var index = _users.IndexOf(existingUser);
            _users[index] = user;
        }
        return Task.FromResult(user);
    }

    public Task UpdateLastLoginAsync(int userId)
    {
        var user = _users.FirstOrDefault(u => u.Id == userId);
        if (user != null)
        {
            user.LastLoginAt = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "FinancialMonitoringSalt"));
        return Convert.ToBase64String(hashedBytes);
    }
}