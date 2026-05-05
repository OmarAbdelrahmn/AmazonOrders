using EmployeeOrderApi.Domain;
using EmployeeOrderApi.Extensions;
using EmployeeOrderApi.Services.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeOrderApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Admin)]
public sealed class ReportController(IReportService reportService) : ControllerBase
{
    /// <summary>
    /// Dashboard KPIs: total/active employees, total/active/today orders, avg duration.
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
        => (await reportService.GetDashboardAsync()).ToActionResult();

    /// <summary>
    /// Per-employee order counts and total minutes worked.
    /// Optional date range filter.
    /// </summary>
    [HttpGet("employees/orders")]
    public async Task<IActionResult> EmployeeOrderCounts(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
        => (await reportService.GetEmployeeOrderCountsAsync(from, to)).ToActionResult();

    /// <summary>
    /// Per-housing summary: employee count, active orders, today's orders.
    /// Includes nested per-employee breakdown.
    /// </summary>
    [HttpGet("housing")]
    public async Task<IActionResult> Housing()
        => (await reportService.GetHousingReportAsync()).ToActionResult();

    /// <summary>
    /// Breakdown by service type: total orders, active orders, avg/total duration.
    /// Optional date range filter.
    /// </summary>
    [HttpGet("services")]
    public async Task<IActionResult> Services(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
        => (await reportService.GetServiceReportAsync(from, to)).ToActionResult();

    /// <summary>
    /// Day-by-day order summary between two dates.
    /// </summary>
    [HttpGet("daily")]
    public async Task<IActionResult> Daily(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        if (from > to)
            return BadRequest(new Extensions.ProblemDetail("Report.InvalidRange", "'from' must be before 'to'."));

        return (await reportService.GetDailyReportAsync(from, to)).ToActionResult();
    }

    /// <summary>
    /// Per-supervisor activity: orders created, active orders, last activity timestamp.
    /// Optional date range filter.
    /// </summary>
    [HttpGet("supervisors")]
    public async Task<IActionResult> SupervisorActivity(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
        => (await reportService.GetSupervisorActivityAsync(from, to)).ToActionResult();

    /// <summary>
    /// All currently active orders (IsOrder = true), sorted by StartedAt ASC.
    /// </summary>
    [HttpGet("orders/active")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Supervisor}")]
    public async Task<IActionResult> ActiveOrders()
        => (await reportService.GetActiveOrdersAsync()).ToActionResult();

    /// <summary>
    /// Closed orders where EndedAt is null (day ended without a follow-up order).
    /// Optionally filter by a specific date.
    /// </summary>
    [HttpGet("orders/null-end")]
    public async Task<IActionResult> NullEndOrders([FromQuery] DateTime? date)
        => (await reportService.GetOrdersWithNullEndAsync(date)).ToActionResult();
}
