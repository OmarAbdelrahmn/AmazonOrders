namespace EmployeeOrderApi.Contracts.Auth;

// ── Requests ──────────────────────────────────────────────────────────────────
public record LoginRequest(
    string Username,
    string Password);

public record RegisterRequest(
    string Username,
    string Email,
    string Password,
    string DisplayName,
    string Role);          // "Admin" | "Supervisor"

public record ChangePasswordRequest(
    string Username,
    string CurrentPassword,
    string NewPassword);

// ── Responses ─────────────────────────────────────────────────────────────────
public record AuthResponse(
    string  Token,
    string  Username,
    string  DisplayName,
    string  Role,
    DateTime ExpiresAt);

public record UserResponse(
    string  Id,
    string  Username,
    string  Email,
    string  DisplayName,
    string  Role,
    bool    IsActive,
    DateTime CreatedAt);
