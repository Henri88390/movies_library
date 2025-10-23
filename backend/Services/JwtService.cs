using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using backend.Models;

namespace backend.Services;

public interface IJwtService
{
    string GenerateToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
    DateTime GetTokenExpiryTime();
    DateTime GetRefreshTokenExpiryTime();
}

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    // Centralized expiry configuration
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(5); // 5 minutes for JWT
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30); // 30 days for refresh token

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public DateTime GetTokenExpiryTime() => DateTime.UtcNow.Add(TokenLifetime);
    public DateTime GetRefreshTokenExpiryTime() => DateTime.UtcNow.Add(RefreshTokenLifetime);

    public string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;
        var expiry = now.Add(TokenLifetime);
        var jti = Guid.NewGuid().ToString();

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, jti),
            new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim(JwtRegisteredClaimNames.Nbf, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email!),
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            notBefore: now,
            expires: expiry,
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        // Log token generation for debugging
        Console.WriteLine($"🔑 Generated JWT token for user {user.Email}:");
        Console.WriteLine($"   Token: {tokenString.Substring(0, 30)}...");
        Console.WriteLine($"   JTI: {jti}");
        Console.WriteLine($"   Issued: {now:yyyy-MM-dd HH:mm:ss.fff}");
        Console.WriteLine($"   Expires: {expiry:yyyy-MM-dd HH:mm:ss.fff}");
        Console.WriteLine($"   Unix timestamp: {new DateTimeOffset(now).ToUnixTimeSeconds()}");

        return tokenString;
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var tokenHandler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            return principal;
        }
        catch
        {
            return null;
        }
    }
}