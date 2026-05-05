using Microsoft.AspNetCore.Identity;

namespace EmployeeOrderApi.Domain;

public sealed class ApplicationUser : IdentityUser
{
    public string  DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow.AddHours(3);
    public bool    IsActive    { get; set; } = true;
}

public static class Roles
{
    public const string Admin      = "Admin";
    public const string Supervisor = "Supervisor";
    public static readonly string[] All = [Admin, Supervisor];
}
