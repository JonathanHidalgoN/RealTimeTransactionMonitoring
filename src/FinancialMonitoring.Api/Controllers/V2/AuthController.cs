using System.Security.Claims;
using FinancialMonitoring.Models;
using FinancialMonitoring.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinancialMonitoring.Api.Controllers.V2;

/// <summary>
/// Authentication controller for user login, registration, and token management
/// </summary>
[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHashingService _passwordHashingService;

    /// <summary>
    /// Initializes a new instance of the AuthController
    /// </summary>
    public AuthController(
        ILogger<AuthController> logger,
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        IPasswordHashingService passwordHashingService)
    {
        _logger = logger;
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _passwordHashingService = passwordHashingService;
    }

    /// <summary>
    /// Authenticates a user and returns JWT tokens
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>Access token, refresh token, and user information</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var user = await _userRepository.GetByUsernameAsync(request.Username);
            if (user == null)
            {
                _logger.LogWarning("Login attempt with invalid username: {Username}", request.Username);
                return Unauthorized(ApiErrorResponse.FromProblemDetails(
                    Models.ProblemDetails.Unauthorized("Invalid username or password")));
            }

            if (!_passwordHashingService.VerifyPassword(request.Password, user.PasswordHash, user.Salt))
            {
                _logger.LogWarning("Login attempt with invalid password for user: {Username}", request.Username);
                return Unauthorized(ApiErrorResponse.FromProblemDetails(
                    Models.ProblemDetails.Unauthorized("Invalid username or password")));
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Login attempt for inactive user: {Username}", request.Username);
                return Unauthorized(ApiErrorResponse.FromProblemDetails(
                    Models.ProblemDetails.Unauthorized("User account is inactive")));
            }

            var accessToken = _jwtTokenService.GenerateAccessToken(user);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();
            var expiresAt = _jwtTokenService.GetAccessTokenExpiration();

            await _userRepository.UpdateLastLoginAsync(user.Id);

            var response = new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                User = UserInfo.FromAuthUser(user)
            };

            _logger.LogInformation("Successful login for user: {Username}", request.Username);
            return Ok(ApiResponse<LoginResponse>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user: {Username}", request.Username);
            return StatusCode(500, ApiErrorResponse.FromProblemDetails(
                Models.ProblemDetails.InternalServerError("An error occurred during login")));
        }
    }

    /// <summary>
    /// Refreshes an access token using a refresh token
    /// </summary>
    /// <param name="request">Refresh token</param>
    /// <returns>New access token and refresh token</returns>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<RefreshTokenResponse>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<ActionResult<ApiResponse<RefreshTokenResponse>>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var userId = _jwtTokenService.ValidateRefreshToken(request.RefreshToken);
            if (userId == null)
            {
                _logger.LogWarning("Invalid refresh token provided");
                return Unauthorized(ApiErrorResponse.FromProblemDetails(
                    Models.ProblemDetails.Unauthorized("Invalid refresh token")));
            }

            var user = await _userRepository.GetByIdAsync(userId.Value);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Refresh token for invalid or inactive user: {UserId}", userId);
                return Unauthorized(ApiErrorResponse.FromProblemDetails(
                    Models.ProblemDetails.Unauthorized("User not found or inactive")));
            }

            _jwtTokenService.InvalidateRefreshToken(request.RefreshToken);
            var newAccessToken = _jwtTokenService.GenerateAccessToken(user);
            var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
            var expiresAt = _jwtTokenService.GetAccessTokenExpiration();

            var response = new RefreshTokenResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = expiresAt
            };

            _logger.LogInformation("Token refreshed for user: {UserId}", userId);
            return Ok(ApiResponse<RefreshTokenResponse>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, ApiErrorResponse.FromProblemDetails(
                Models.ProblemDetails.InternalServerError("An error occurred during token refresh")));
        }
    }

    /// <summary>
    /// Logs out a user by invalidating their refresh token
    /// </summary>
    /// <param name="request">Refresh token to invalidate</param>
    /// <returns>Success confirmation</returns>
    [HttpPost("logout")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    public ActionResult<ApiResponse<object>> Logout([FromBody] RefreshTokenRequest request)
    {
        try
        {
            _jwtTokenService.InvalidateRefreshToken(request.RefreshToken);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User logged out: {UserId}", userId);

            return Ok(ApiResponse<object>.SuccessResponse(new { message = "Logged out successfully" }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, ApiErrorResponse.FromProblemDetails(
                Models.ProblemDetails.InternalServerError("An error occurred during logout")));
        }
    }

    /// <summary>
    /// Registers a new user
    /// </summary>
    /// <param name="request">User registration data</param>
    /// <returns>Created user information</returns>
    [HttpPost("register")]
    [Authorize(Policy = AppConstants.AdminRole, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(ApiResponse<RegisterResponse>), 201)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 409)]
    public async Task<ActionResult<ApiResponse<RegisterResponse>>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var existingUserByUsername = await _userRepository.GetByUsernameAsync(request.Username);
            if (existingUserByUsername != null)
            {
                return Conflict(ApiErrorResponse.FromProblemDetails(
                    Models.ProblemDetails.ValidationError("Username already exists")));
            }

            var existingUserByEmail = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUserByEmail != null)
            {
                return Conflict(ApiErrorResponse.FromProblemDetails(
                    Models.ProblemDetails.ValidationError("Email already exists")));
            }

            var salt = _passwordHashingService.GenerateRandomSalt();
            var newUser = new AuthUser
            {
                Username = request.Username,
                Email = request.Email,
                Salt = salt,
                PasswordHash = _passwordHashingService.HashPassword(request.Password, salt),
                Role = request.Role,
                FirstName = request.FirstName,
                LastName = request.LastName,
                IsActive = true
            };

            var createdUser = await _userRepository.CreateAsync(newUser);

            var response = new RegisterResponse
            {
                Id = createdUser.Id,
                Username = createdUser.Username,
                Email = createdUser.Email,
                Role = createdUser.Role,
                CreatedAt = createdUser.CreatedAt
            };

            var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User {Username} registered by admin {AdminId}", request.Username, adminUserId);

            return CreatedAtAction(nameof(GetMe), new { }, ApiResponse<RegisterResponse>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration");
            return StatusCode(500, ApiErrorResponse.FromProblemDetails(
                Models.ProblemDetails.InternalServerError("An error occurred during registration")));
        }
    }

    /// <summary>
    /// Gets current authenticated user information
    /// </summary>
    /// <returns>Current user information</returns>
    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(ApiResponse<UserInfo>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<ActionResult<ApiResponse<UserInfo>>> GetMe()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiErrorResponse.FromProblemDetails(
                    Models.ProblemDetails.Unauthorized("Invalid user token")));
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(ApiErrorResponse.FromProblemDetails(
                    Models.ProblemDetails.NotFound("User not found")));
            }

            var userInfo = UserInfo.FromAuthUser(user);
            return Ok(ApiResponse<UserInfo>.SuccessResponse(userInfo));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user information");
            return StatusCode(500, ApiErrorResponse.FromProblemDetails(
                Models.ProblemDetails.InternalServerError("An error occurred retrieving user information")));
        }
    }

}

