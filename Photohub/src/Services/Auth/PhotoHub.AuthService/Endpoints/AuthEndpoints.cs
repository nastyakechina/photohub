using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PhotoHub.AuthService.Application;
using PhotoHub.AuthService.Domain.Users;
using PhotoHub.AuthService.Infrastructure.Persistence;

namespace PhotoHub.AuthService.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/register", RegisterAsync)
            .WithName("Register");

        group.MapPost("/login", LoginAsync)
            .WithName("Login");

        group.MapGet("/users", GetAllUsersAsync)
            .WithName("GetAllUsers");

        return group;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        AuthDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateRegisterRequest(request);
        if (validationError is not null)
        {
            return Results.BadRequest(new { error = validationError });
        }

        var userName = request.UserName.Trim();
        var email = request.Email.Trim();

        var emailExists = await dbContext.Users
            .AnyAsync(user => user.Email == email, cancellationToken);

        if (emailExists)
        {
            return Results.Conflict(new { error = "Email is already used." });
        }

        var userNameExists = await dbContext.Users
            .AnyAsync(user => user.UserName == userName, cancellationToken);

        if (userNameExists)
        {
            return Results.Conflict(new { error = "UserName is already used." });
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var user = User.Create(userName, email, passwordHash);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new AuthUserResponse(
            user.Id,
            user.UserName,
            user.Email));
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        AuthDbContext dbContext,
        IOptions<JwtOptions> jwtOptions,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateLoginRequest(request);
        if (validationError is not null)
        {
            return Results.BadRequest(new { error = validationError });
        }

        var email = request.Email.Trim();

        var user = await dbContext.Users
            .FirstOrDefaultAsync(user => user.Email == email, cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Results.Unauthorized();
        }

        var accessToken = CreateAccessToken(user, jwtOptions.Value);

        return Results.Ok(new LoginResponse(
            accessToken,
            user.Id,
            user.UserName,
            user.Email));
    }

    private static string? ValidateRegisterRequest(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            return "UserName is required.";
        }

        var userNameLength = request.UserName.Trim().Length;
        if (userNameLength < 3 || userNameLength > 64)
        {
            return "UserName must be between 3 and 64 characters.";
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return "Email is required.";
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
        {
            return "Password must contain at least 6 characters.";
        }

        return null;
    }

    private static string? ValidateLoginRequest(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return "Email is required.";
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return "Password is required.";
        }

        return null;
    }

    private static string CreateAccessToken(User user, JwtOptions options)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(options.ExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static async Task<IResult> GetAllUsersAsync(
        AuthDbContext dbContext,
        int page = 1,
        int pageSize = 10,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 50) pageSize = 10;

        var query = dbContext.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => EF.Functions.ILike(u.UserName, $"%{search}%"));

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var users = await query
            .OrderBy(u => u.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AuthUserResponse(u.Id, u.UserName, u.Email))
            .ToListAsync(cancellationToken);

        return Results.Ok(new
        {
            users,
            page,
            pageSize,
            totalCount,
            totalPages
        });
    }
}

public sealed record RegisterRequest(
    string UserName,
    string Email,
    string Password);

public sealed record AuthUserResponse(
    Guid UserId,
    string UserName,
    string Email);

public sealed record LoginRequest(
    string Email,
    string Password);

public sealed record LoginResponse(
    string AccessToken,
    Guid UserId,
    string UserName,
    string Email);
