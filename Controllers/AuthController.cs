using EmployeeOrderApi.Contracts.Auth;
using EmployeeOrderApi.Domain;
using EmployeeOrderApi.Extensions;
using EmployeeOrderApi.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeOrderApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>Login and receive a JWT token.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
        => (await authService.LoginAsync(request)).ToActionResult();

    /// <summary>Register a new user. Admin only.</summary>
    [HttpPost("register")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        => (await authService.RegisterAsync(request)).ToActionResult();

    /// <summary>Change password.</summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        => (await authService.ChangePasswordAsync(request)).ToActionResult();

    /// <summary>Get all users. Admin only.</summary>
    [HttpGet("users")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> GetAllUsers()
        => (await authService.GetAllUsersAsync()).ToActionResult();

    /// <summary>Toggle a user's active status. Admin only.</summary>
    [HttpPatch("users/{username}/toggle")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> ToggleUser(string username)
        => (await authService.ToggleUserActiveAsync(username)).ToActionResult();
}
