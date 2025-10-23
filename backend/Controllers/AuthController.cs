using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.DTOs;
using backend.Models;
using backend.Services;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IJwtService jwtService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto registerDto)
    {
        try
        {
            var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "User with this email already exists." });
            }

            var user = new User
            {
                UserName = registerDto.Email,
                Email = registerDto.Email,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded)
            {
                return BadRequest(new { message = "Failed to create user.", errors = result.Errors });
            }

            var token = _jwtService.GenerateToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            // Store refresh token
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = _jwtService.GetRefreshTokenExpiryTime();
            await _userManager.UpdateAsync(user);

            return Ok(new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                Email = user.Email!,
                ExpiresAt = _jwtService.GetTokenExpiryTime()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during user registration");
            return StatusCode(500, new { message = "An error occurred during registration." });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto loginDto)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
            if (!result.Succeeded)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            // Update last login time
            user.LastLoginAt = DateTime.UtcNow;

            // Generate tokens
            var token = _jwtService.GenerateToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            // Store refresh token
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = _jwtService.GetRefreshTokenExpiryTime();
            await _userManager.UpdateAsync(user);

            return Ok(new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                Email = user.Email!,
                ExpiresAt = _jwtService.GetTokenExpiryTime()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during user login");
            return StatusCode(500, new { message = "An error occurred during login." });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Invalid token." });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            return Ok(new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting current user");
            return StatusCode(500, new { message = "An error occurred while retrieving user information." });
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous] // No JWT required, we validate refresh token instead
    public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        try
        {
            _logger.LogInformation("Refresh token request received");

            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                _logger.LogWarning("Refresh token request missing refresh token");
                return BadRequest(new { message = "Refresh token is required." });
            }

            _logger.LogInformation("Looking for user with refresh token: {RefreshToken}",
                request.RefreshToken.Substring(0, Math.Min(10, request.RefreshToken.Length)) + "...");

            // Find user by refresh token
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken);

            if (user == null)
            {
                _logger.LogWarning("No user found with provided refresh token");
                return Unauthorized(new { message = "Invalid refresh token." });
            }

            _logger.LogInformation("User found: {UserId}, checking token expiry", user.Id);

            // Check if refresh token is still valid
            if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                _logger.LogWarning("Refresh token expired for user {UserId}. Expiry: {ExpiryTime}, Now: {CurrentTime}",
                    user.Id, user.RefreshTokenExpiryTime, DateTime.UtcNow);
                return Unauthorized(new { message = "Refresh token expired." });
            }

            _logger.LogInformation("Refresh token valid, generating new JWT for user {UserId}", user.Id);

            // Generate new JWT token only (no refresh token rotation)
            var token = _jwtService.GenerateToken(user);

            // Keep the same refresh token, just extend its expiry
            user.RefreshTokenExpiryTime = _jwtService.GetRefreshTokenExpiryTime();
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Successfully refreshed JWT for user {UserId}", user.Id);

            return Ok(new AuthResponseDto
            {
                Token = token,
                RefreshToken = user.RefreshToken, // Return SAME refresh token (no rotation)
                Email = user.Email!,
                ExpiresAt = _jwtService.GetTokenExpiryTime()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during token refresh");
            return StatusCode(500, new { message = "An error occurred during token refresh." });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    // Invalidate refresh token
                    user.RefreshToken = null;
                    user.RefreshTokenExpiryTime = null;
                    await _userManager.UpdateAsync(user);
                }
            }

            await _signInManager.SignOutAsync();
            return Ok(new { message = "Logged out successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during logout");
            return StatusCode(500, new { message = "An error occurred during logout." });
        }
    }
}