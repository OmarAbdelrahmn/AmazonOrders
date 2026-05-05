using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EmployeeOrderApi.Contracts.Auth;
using EmployeeOrderApi.Domain;
using EmployeeOrderApi.Services.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace EmployeeOrderApi.Services.Auth;

// ── Interface ─────────────────────────────────────────────────────────────────
public interface IAuthService
{
    Task<Result<AuthResponse>>      LoginAsync(LoginRequest request);
    Task<Result<UserResponse>>      RegisterAsync(RegisterRequest request);
    Task<Result>                    ChangePasswordAsync(ChangePasswordRequest request);
    Task<Result<IEnumerable<UserResponse>>> GetAllUsersAsync();
    Task<Result>                    ToggleUserActiveAsync(string username);
}

// ── Implementation ────────────────────────────────────────────────────────────
public sealed class AuthService(
    UserManager<ApplicationUser>  userManager,
    RoleManager<IdentityRole>     roleManager,
    IConfiguration                config) : IAuthService
{
    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request)
    {
        var user = await userManager.FindByNameAsync(request.Username);
        if (user is null || !user.IsActive)
            return Result.Failure<AuthResponse>(
                new Error("Auth.InvalidCredentials", "Invalid username or password.", 401));

        if (!await userManager.CheckPasswordAsync(user, request.Password))
            return Result.Failure<AuthResponse>(
                new Error("Auth.InvalidCredentials", "Invalid username or password.", 401));

        var roles = await userManager.GetRolesAsync(user);
        var role  = roles.FirstOrDefault() ?? string.Empty;
        var token = GenerateJwt(user, role);

        return Result.Success(new AuthResponse(
            Token:       token.Token,
            Username:    user.UserName!,
            DisplayName: user.DisplayName,
            Role:        role,
            ExpiresAt:   token.ExpiresAt));
    }

    public async Task<Result<UserResponse>> RegisterAsync(RegisterRequest request)
    {
        if (!Roles.All.Contains(request.Role))
            return Result.Failure<UserResponse>(
                new Error("Auth.InvalidRole",
                    $"Role must be one of: {string.Join(", ", Roles.All)}", 400));

        if (await userManager.FindByNameAsync(request.Username) is not null)
            return Result.Failure<UserResponse>(
                new Error("Auth.UserExists", "A user with this username already exists.", 409));

        if (await userManager.FindByEmailAsync(request.Email) is not null)
            return Result.Failure<UserResponse>(
                new Error("Auth.EmailExists", "A user with this email already exists.", 409));

        var user = new ApplicationUser
        {
            UserName    = request.Username,
            Email       = request.Email,
            DisplayName = request.DisplayName,
            IsActive    = true
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
            return Result.Failure<UserResponse>(new Error("Auth.CreateFailed", errors, 400));
        }

        await userManager.AddToRoleAsync(user, request.Role);

        return Result.Success(new UserResponse(
            Id:          user.Id,
            Username:    user.UserName!,
            Email:       user.Email!,
            DisplayName: user.DisplayName,
            Role:        request.Role,
            IsActive:    true,
            CreatedAt:   user.CreatedAt));
    }

    public async Task<Result> ChangePasswordAsync(ChangePasswordRequest request)
    {
        var user = await userManager.FindByNameAsync(request.Username);
        if (user is null)
            return Result.Failure(new Error("Auth.NotFound", "User not found.", 404));

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure(new Error("Auth.PasswordChangeFailed", errors, 400));
        }
        return Result.Success();
    }

    public async Task<Result<IEnumerable<UserResponse>>> GetAllUsersAsync()
    {
        var users = userManager.Users.ToList();
        var responses = new List<UserResponse>();

        foreach (var u in users)
        {
            var roles = await userManager.GetRolesAsync(u);
            responses.Add(new UserResponse(
                Id:          u.Id,
                Username:    u.UserName!,
                Email:       u.Email!,
                DisplayName: u.DisplayName,
                Role:        roles.FirstOrDefault() ?? "-",
                IsActive:    u.IsActive,
                CreatedAt:   u.CreatedAt));
        }
        return Result.Success<IEnumerable<UserResponse>>(responses);
    }

    public async Task<Result> ToggleUserActiveAsync(string username)
    {
        var user = await userManager.FindByNameAsync(username);
        if (user is null)
            return Result.Failure(new Error("Auth.NotFound", "User not found.", 404));

        user.IsActive = !user.IsActive;
        await userManager.UpdateAsync(user);
        return Result.Success();
    }

    // ── JWT helpers ───────────────────────────────────────────────────────────
    private (string Token, DateTime ExpiresAt) GenerateJwt(ApplicationUser user, string role)
    {
        var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(int.Parse(config["Jwt:ExpiryHours"] ?? "8"));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name,           user.UserName!),
            new(ClaimTypes.Email,          user.Email!),
            new(ClaimTypes.Role,           role),
            new("DisplayName",             user.DisplayName),
        };

        var token = new JwtSecurityToken(
            issuer:            config["Jwt:Issuer"],
            audience:          config["Jwt:Audience"],
            claims:            claims,
            expires:           expires,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
