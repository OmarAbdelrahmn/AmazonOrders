using System.Security.Claims;
using EmployeeOrderApi.Contracts.Order;
using EmployeeOrderApi.Domain;
using EmployeeOrderApi.Extensions;
using EmployeeOrderApi.Services.Order;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeOrderApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class OrderController(IOrderService orderService) : ControllerBase
{
    private string CurrentUser =>
        User.FindFirstValue(ClaimTypes.Name) ?? "system";

    /// <summary>
    /// Supervisor assigns a new order to an employee.
    /// Automatically closes the previous active order for the same employee:
    /// same day → previous EndedAt = new StartedAt | different day → EndedAt stays null.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Supervisor}")]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
        => (await orderService.CreateAsync(request, CurrentUser)).ToActionResult();

    /// <summary>Manually close an employee's active order without assigning a new one.</summary>
    [HttpPost("{iqamaNo:long}/close")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Supervisor}")]
    public async Task<IActionResult> CloseActive(long iqamaNo)
        => (await orderService.CloseActiveOrderAsync(iqamaNo, CurrentUser)).ToActionResult();

    /// <summary>Get a single order by ID.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
        => (await orderService.GetByIdAsync(id)).ToActionResult();

    /// <summary>Get the currently active order for an employee.</summary>
    [HttpGet("active/{iqamaNo:long}")]
    public async Task<IActionResult> GetActive(long iqamaNo)
        => (await orderService.GetActiveOrderAsync(iqamaNo)).ToActionResult();

    /// <summary>Get full order history for a specific employee.</summary>
    [HttpGet("employee/{iqamaNo:long}")]
    public async Task<IActionResult> GetEmployeeOrders(long iqamaNo)
        => (await orderService.GetEmployeeOrdersAsync(iqamaNo)).ToActionResult();

    /// <summary>Filtered, paginated order list.</summary>
    [HttpGet]
    public async Task<IActionResult> Filter([FromQuery] OrderFilterRequest filter)
        => (await orderService.FilterAsync(filter)).ToActionResult();
}
