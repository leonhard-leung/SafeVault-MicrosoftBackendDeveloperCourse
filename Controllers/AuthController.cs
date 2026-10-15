using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.IdentityModel.Tokens;
using SafeVaultMicrosoftCourse.Models;
using SafeVaultMicrosoftCourse.Services;

namespace SafeVaultMicrosoftCourse.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserRepository _userRepository;
    private readonly IConfiguration _configuration;

    public AuthController(UserRepository userRepository, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterRequest request)
    {
        if (!InputValidator.IsValidUsername(request.Username))
        {
            return BadRequest(new { error = "Invalid username." });
        }
        
        if (!InputValidator.IsValidEmail(request.Email))
        {
            return BadRequest(new { error = "Invalid email address." });
        }

        if (string.IsNullOrEmpty(request.Password) ||
            request.Password.Length < 12 ||
            request.Password.Length > 72)
        {
            return BadRequest(new { error = "Password must be between 12 and 72 characters." });
        }

        var username = request.Username!;
        var email = request.Email!.Trim();
        
        if (_userRepository.UserExists(username))
            return Conflict(new { error = "Username already exists." });

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        try
        {
            var userId = _userRepository.CreateUser(
                username,
                email,
                passwordHash,
                "user");
            
            return Created(
                $"/api/users/{userId}",
                new
                {
                    username,
                    email,
                    passwordHash,
                    role = "user"
                });
        }
        catch (SqliteException ex) when (ex.SqliteExtendedErrorCode == 2067)
        {
            return Conflict(new { error = "Username already exists." });
        }
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrEmpty(request.Password))
        {
            return Unauthorized(new { error = "Invalid username or password." });
        }

        var user = _userRepository.GetUserByUsername(request.Username);

        if (user is null ||
            user.PasswordHash is null ||
            !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { error = "Invalid username or password." });
        }

        var key = _configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(key) ||
            Encoding.UTF8.GetByteCount(key) < 32)
        {
            throw new InvalidOperationException("JWT signing key must be configured with at least 32 bytes.");
        }

        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                user.UserID.ToString()),
            new Claim(ClaimTypes.Name, user.Username!),
            new Claim(ClaimTypes.Role, user.Role!)
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials
        );

        return Ok(new
        {
            accessToken = new JwtSecurityTokenHandler().WriteToken(token),
            tokenType = "Bearer",
            expiresInMinutes = 30
        });
    }
}