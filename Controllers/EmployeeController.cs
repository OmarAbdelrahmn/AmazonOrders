using System.Security.Claims;
using EmployeeOrderApi.Contracts.Employee;
using EmployeeOrderApi.Domain;
using EmployeeOrderApi.Extensions;
using EmployeeOrderApi.Services.Employee;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeOrderApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class EmployeeController(IEmployeeService employeeService) : ControllerBase
{
    private string CurrentUser =>
        User.FindFirstValue(ClaimTypes.Name) ?? "system";

    // ── CRUD ──────────────────────────────────────────────────────────────────

    /// <summary>Create a new employee. Optionally upload a profile image.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create(
        [FromForm] CreateEmployeeRequest request,
        IFormFile?                       image)
        => (await employeeService.CreateAsync(request, image, CurrentUser)).ToActionResult();

    /// <summary>Update an employee's details.</summary>
    [HttpPut("{iqamaNo:long}")]
    [Authorize(Roles = Roles.Admin)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(
        long                   iqamaNo,
        [FromForm] UpdateEmployeeRequest request,
        IFormFile?             image)
        => (await employeeService.UpdateAsync(iqamaNo, request, image, CurrentUser)).ToActionResult();

    /// <summary>Soft-delete an employee.</summary>
    [HttpDelete("{iqamaNo:long}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(long iqamaNo)
        => (await employeeService.DeleteAsync(iqamaNo, CurrentUser)).ToActionResult();

    /// <summary>Restore a soft-deleted employee.</summary>
    [HttpPatch("{iqamaNo:long}/restore")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Restore(long iqamaNo)
        => (await employeeService.RestoreAsync(iqamaNo)).ToActionResult();

    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>Get full employee details including active order.</summary>
    [HttpGet("{iqamaNo:long}")]
    public async Task<IActionResult> GetByIqama(long iqamaNo)
        => (await employeeService.GetByIqamaAsync(iqamaNo)).ToActionResult();

    /// <summary>Paginated list with filters.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] EmployeeSearchFilter filter)
        => (await employeeService.GetAllAsync(filter)).ToActionResult();

    /// <summary>Smart search across name (AR/EN), IqamaNo, RiderId, HousingName.</summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return BadRequest(new ProblemDetail("Search.EmptyKeyword", "Keyword cannot be empty."));

        return (await employeeService.SearchAsync(keyword)).ToActionResult();
    }
}
