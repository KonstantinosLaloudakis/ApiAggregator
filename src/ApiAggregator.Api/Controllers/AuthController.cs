using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ApiAggregator.Api.Configuration;
using ApiAggregator.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ApiAggregator.Api.Controllers;

/// <summary>
/// Controller for authentication - generates JWT tokens
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthController> _logger;

    // In-memory user store (for assignment purposes)
    private static readonly Dictionary<string, string> Users = new()
    {
        { "admin", "password" }
    };

    public AuthController(IOptions<JwtSettings> jwtSettings, ILogger<AuthController> logger)
    {
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Generates a JWT token for authentication
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>JWT token if credentials are valid</returns>
    /// <response code="200">Returns the JWT token</response>
    /// <response code="401">If credentials are invalid</response>
    [HttpPost("token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<TokenResponse> GenerateToken([FromBody] LoginRequest request)
    {
        _logger.LogInformation("Token request received for user: {Username}", request.Username);

        // Validate credentials
        if (!ValidateCredentials(request.Username, request.Password))
        {
            _logger.LogWarning("Invalid credentials for user: {Username}", request.Username);
            return Unauthorized(new ErrorResponse { Message = "Invalid username or password", StatusCode = 401 });
        }

        // Generate token
        var token = CreateToken(request.Username);
        var expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

        _logger.LogInformation("Token generated for user: {Username}, expires: {Expiration}", request.Username, expiration);

        return Ok(new TokenResponse
        {
            Token = token,
            Expiration = expiration,
            TokenType = "Bearer"
        });
    }

    private bool ValidateCredentials(string username, string password)
    {
        return Users.TryGetValue(username, out var storedPassword) && storedPassword == password;
    }

    private string CreateToken(string username)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, "User"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
