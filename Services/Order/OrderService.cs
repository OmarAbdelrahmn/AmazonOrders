using EmployeeOrderApi.Contracts.Order;
using EmployeeOrderApi.Data;
using EmployeeOrderApi.Domain;
using EmployeeOrderApi.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace EmployeeOrderApi.Services.Order;

// ── Interface ─────────────────────────────────────────────────────────────────
public interface IOrderService
{
    /// <summary>
    /// Supervisor assigns a new order to an employee.
    /// Previous active order for the same employee is closed automatically:
    ///   • Same calendar day  → EndedAt = new order's StartedAt
    ///   • Different day      → EndedAt stays null (day ended without a follow-up)
    /// </summary>
    Task<Result<OrderResponse>>              CreateAsync(CreateOrderRequest request, string requestedBy);

    /// <summary>Manually close the active order for an employee (end of shift, etc.).</summary>
    Task<Result>                             CloseActiveOrderAsync(long iqamaNo, string closedBy);

    Task<Result<OrderResponse>>              GetByIdAsync(int id);
    Task<Result<ActiveOrderResponse>>        GetActiveOrderAsync(long iqamaNo);
    Task<Result<IEnumerable<OrderResponse>>> GetEmployeeOrdersAsync(long iqamaNo);
    Task<Result<Contracts.Employee.PagedList<OrderResponse>>> FilterAsync(OrderFilterRequest filter);
}

// ── Implementation ────────────────────────────────────────────────────────────
public sealed class OrderService(AppDbContext db) : IOrderService
{
    public async Task<Result<OrderResponse>> CreateAsync(CreateOrderRequest request, string requestedBy)
    {
        // 1. Validate employee exists (bypass soft-delete so we can give a clear error)
        var employee = await db.Employees
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.IqamaNo == request.IqamaNo);

        if (employee is null)
            return Result.Failure<OrderResponse>(
                new Error("Employee.NotFound", $"Employee with IqamaNo {request.IqamaNo} not found.", 404));

        if (employee.IsDeleted)
            return Result.Failure<OrderResponse>(
                new Error("Employee.Deleted", "Cannot assign an order to a deleted employee.", 400));

        var now = DateTime.UtcNow.AddHours(3); // local time (UTC+3)

        // 2. Close the previous active order if one exists
        var previousOrder = await db.Orders
            .Where(o => o.IqamaNo == request.IqamaNo && o.IsOrder)
            .FirstOrDefaultAsync();

        if (previousOrder is not null)
        {
            // Only set EndedAt if the previous order started on the SAME calendar day
            bool sameDay = previousOrder.StartedAt.Date == now.Date;
            if (sameDay)
                previousOrder.EndedAt = now;   // EndedAt = new order's StartedAt (they're equal here)
            // else: EndedAt stays null → day ended without a follow-up order

            previousOrder.IsOrder = false;
        }

        // 3. Create the new active order
        var order = new Domain.Order
        {
            IqamaNo     = request.IqamaNo,
            IsOrder     = true,
            Service     = request.Service,
            StartedAt   = now,
            EndedAt     = null,
            RequestedBy = requestedBy,
            Notes       = request.Notes,
            CreatedAt   = now
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        return Result.Success(MapToResponse(order, employee));
    }

    public async Task<Result> CloseActiveOrderAsync(long iqamaNo, string closedBy)
    {
        var order = await db.Orders
            .Where(o => o.IqamaNo == iqamaNo && o.IsOrder)
            .FirstOrDefaultAsync();

        if (order is null)
            return Result.Failure(
                new Error("Order.NoActive", $"No active order found for IqamaNo {iqamaNo}.", 404));

        order.IsOrder = false;
        order.EndedAt = null; // closed by admin/supervisor without a follow-up → null (day ended)

        await db.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<OrderResponse>> GetByIdAsync(int id)
    {
        var order = await db.Orders
            .Include(o => o.Employee)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
            return Result.Failure<OrderResponse>(
                new Error("Order.NotFound", $"Order {id} was not found.", 404));

        return Result.Success(MapToResponse(order, order.Employee));
    }

    public async Task<Result<ActiveOrderResponse>> GetActiveOrderAsync(long iqamaNo)
    {
        var order = await db.Orders
            .Include(o => o.Employee)
            .Where(o => o.IqamaNo == iqamaNo && o.IsOrder)
            .FirstOrDefaultAsync();

        if (order is null)
            return Result.Failure<ActiveOrderResponse>(
                new Error("Order.NoActive", $"Employee {iqamaNo} has no active order.", 404));

        var elapsed = (DateTime.UtcNow.AddHours(3) - order.StartedAt).TotalMinutes;

        return Result.Success(new ActiveOrderResponse(
            Id:              order.Id,
            IqamaNo:         order.IqamaNo,
            EmployeeNameAR:  order.Employee.NameAR,
            EmployeeNameEN:  order.Employee.NameEN,
            HousingName:     order.Employee.HousingName,
            Service:         order.Service,
            StartedAt:       order.StartedAt,
            ElapsedMinutes:  Math.Round(elapsed, 1),
            RequestedBy:     order.RequestedBy,
            Notes:           order.Notes));
    }

    public async Task<Result<IEnumerable<OrderResponse>>> GetEmployeeOrdersAsync(long iqamaNo)
    {
        var employee = await db.Employees
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.IqamaNo == iqamaNo);

        if (employee is null)
            return Result.Failure<IEnumerable<OrderResponse>>(
                new Error("Employee.NotFound", $"Employee {iqamaNo} not found.", 404));

        var orders = await db.Orders
            .Where(o => o.IqamaNo == iqamaNo)
            .OrderByDescending(o => o.StartedAt)
            .ToListAsync();

        return Result.Success<IEnumerable<OrderResponse>>(
            orders.Select(o => MapToResponse(o, employee)));
    }

    public async Task<Result<Contracts.Employee.PagedList<OrderResponse>>> FilterAsync(OrderFilterRequest filter)
    {
        var query = db.Orders
            .Include(o => o.Employee)
            .AsQueryable();

        if (filter.IqamaNo.HasValue)
            query = query.Where(o => o.IqamaNo == filter.IqamaNo.Value);

        if (filter.IsOrder.HasValue)
            query = query.Where(o => o.IsOrder == filter.IsOrder.Value);

        if (!string.IsNullOrWhiteSpace(filter.Service))
            query = query.Where(o => o.Service.Contains(filter.Service));

        if (!string.IsNullOrWhiteSpace(filter.RequestedBy))
            query = query.Where(o => o.RequestedBy.Contains(filter.RequestedBy));

        if (filter.DateFrom.HasValue)
            query = query.Where(o => o.StartedAt >= filter.DateFrom.Value);

        if (filter.DateTo.HasValue)
            query = query.Where(o => o.StartedAt <= filter.DateTo.Value);

        var total = await query.CountAsync();
        var skip  = (filter.Page - 1) * filter.PageSize;

        var orders = await query
            .OrderByDescending(o => o.StartedAt)
            .Skip(skip)
            .Take(filter.PageSize)
            .ToListAsync();

        var data = orders.Select(o => MapToResponse(o, o.Employee)).ToList();

        var paged = new Contracts.Employee.PagedList<OrderResponse>(
            Data:       data,
            TotalCount: total,
            Page:       filter.Page,
            PageSize:   filter.PageSize,
            TotalPages: (int)Math.Ceiling(total / (double)filter.PageSize));

        return Result.Success(paged);
    }

    // ── Mapping ───────────────────────────────────────────────────────────────
    private static OrderResponse MapToResponse(Domain.Order o, Domain.Employee emp)
    {
        TimeSpan? duration = o.EndedAt.HasValue
            ? o.EndedAt.Value - o.StartedAt
            : (TimeSpan?)null;

        return new OrderResponse(
            Id:             o.Id,
            IqamaNo:        o.IqamaNo,
            EmployeeNameAR: emp.NameAR,
            EmployeeNameEN: emp.NameEN,
            HousingName:    emp.HousingName,
            IsOrder:        o.IsOrder,
            Service:        o.Service,
            StartedAt:      o.StartedAt,
            EndedAt:        o.EndedAt,
            Duration:       duration,
            RequestedBy:    o.RequestedBy,
            Notes:          o.Notes,
            CreatedAt:      o.CreatedAt);
    }
}
