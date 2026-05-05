using EmployeeOrderApi.Data;
using EmployeeOrderApi.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace EmployeeOrderApi.Services.Report;

// ── Contracts (report-specific DTOs) ─────────────────────────────────────────
public record DashboardSummary(
    int      TotalEmployees,
    int      ActiveEmployees,
    int      DeletedEmployees,
    int      TotalOrders,
    int      ActiveOrders,
    int      TodayOrders,
    double   AvgOrderDurationMinutes,
    DateTime GeneratedAt);

public record EmployeeOrderCountReport(
    long     IqamaNo,
    string   NameAR,
    string   NameEN,
    string?  HousingName,
    string?  RiderId,
    int      TotalOrders,
    int      ActiveOrders,
    DateTime? LastOrderStartedAt,
    double   TotalMinutesWorked);

public record HousingReport(
    string   HousingName,
    int      EmployeeCount,
    int      ActiveOrders,
    int      TotalOrdersToday,
    List<EmployeeOrderCountReport> Employees);

public record ServiceReport(
    string   Service,
    int      TotalOrders,
    int      ActiveOrders,
    double   AvgDurationMinutes,
    double   TotalMinutes);

public record DailyOrderReport(
    DateOnly Date,
    int      TotalOrders,
    int      UniqueEmployees,
    double   TotalMinutesWorked,
    double   AvgOrdersPerEmployee);

public record SupervisorActivityReport(
    string   SupervisorUsername,
    int      OrdersCreated,
    int      ActiveOrders,
    DateTime? LastActivity);

// ── Interface ─────────────────────────────────────────────────────────────────
public interface IReportService
{
    Task<Result<DashboardSummary>>                      GetDashboardAsync();
    Task<Result<IEnumerable<EmployeeOrderCountReport>>> GetEmployeeOrderCountsAsync(DateTime? from, DateTime? to);
    Task<Result<IEnumerable<HousingReport>>>            GetHousingReportAsync();
    Task<Result<IEnumerable<ServiceReport>>>            GetServiceReportAsync(DateTime? from, DateTime? to);
    Task<Result<IEnumerable<DailyOrderReport>>>         GetDailyReportAsync(DateTime from, DateTime to);
    Task<Result<IEnumerable<SupervisorActivityReport>>> GetSupervisorActivityAsync(DateTime? from, DateTime? to);
    Task<Result<IEnumerable<Contracts.Order.OrderResponse>>> GetActiveOrdersAsync();
    Task<Result<IEnumerable<Contracts.Order.OrderResponse>>> GetOrdersWithNullEndAsync(DateTime? date);
}

// ── Implementation ────────────────────────────────────────────────────────────
public sealed class ReportService(AppDbContext db) : IReportService
{
    public async Task<Result<DashboardSummary>> GetDashboardAsync()
    {
        var now   = DateTime.UtcNow.AddHours(3);
        var today = now.Date;

        var totalEmployees   = await db.Employees.IgnoreQueryFilters().CountAsync();
        var activeEmployees  = await db.Employees.CountAsync(); // query filter removes deleted
        var deletedEmployees = totalEmployees - activeEmployees;
        var totalOrders      = await db.Orders.CountAsync();
        var activeOrders     = await db.Orders.CountAsync(o => o.IsOrder);
        var todayOrders      = await db.Orders.CountAsync(o => o.StartedAt.Date == today);

        // Average duration of COMPLETED orders (EndedAt not null)
        var completedOrders  = await db.Orders
            .Where(o => !o.IsOrder && o.EndedAt.HasValue)
            .Select(o => new { o.StartedAt, EndedAt = o.EndedAt!.Value })
            .ToListAsync();

        double avgDuration = completedOrders.Count == 0 ? 0
            : completedOrders.Average(o => (o.EndedAt - o.StartedAt).TotalMinutes);

        return Result.Success(new DashboardSummary(
            TotalEmployees:          totalEmployees,
            ActiveEmployees:         activeEmployees,
            DeletedEmployees:        deletedEmployees,
            TotalOrders:             totalOrders,
            ActiveOrders:            activeOrders,
            TodayOrders:             todayOrders,
            AvgOrderDurationMinutes: Math.Round(avgDuration, 1),
            GeneratedAt:             now));
    }

    public async Task<Result<IEnumerable<EmployeeOrderCountReport>>> GetEmployeeOrderCountsAsync(
        DateTime? from, DateTime? to)
    {
        var query = db.Orders.AsQueryable();
        if (from.HasValue) query = query.Where(o => o.StartedAt >= from.Value);
        if (to.HasValue)   query = query.Where(o => o.StartedAt <= to.Value);

        var orders = await query
            .Include(o => o.Employee)
            .ToListAsync();

        var grouped = orders
            .GroupBy(o => o.IqamaNo)
            .Select(g =>
            {
                var emp    = g.First().Employee;
                var total  = (double)g
                    .Where(o => !o.IsOrder && o.EndedAt.HasValue)
                    .Sum(o => (o.EndedAt!.Value - o.StartedAt).TotalMinutes);

                return new EmployeeOrderCountReport(
                    IqamaNo:           emp.IqamaNo,
                    NameAR:            emp.NameAR,
                    NameEN:            emp.NameEN,
                    HousingName:       emp.HousingName,
                    RiderId:           emp.RiderId,
                    TotalOrders:       g.Count(),
                    ActiveOrders:      g.Count(o => o.IsOrder),
                    LastOrderStartedAt: g.Max(o => o.StartedAt),
                    TotalMinutesWorked: Math.Round(total, 1));
            })
            .OrderByDescending(r => r.TotalOrders)
            .ToList();

        return Result.Success<IEnumerable<EmployeeOrderCountReport>>(grouped);
    }

    public async Task<Result<IEnumerable<HousingReport>>> GetHousingReportAsync()
    {
        var today     = DateTime.UtcNow.AddHours(3).Date;
        var employees = await db.Employees.ToListAsync(); // active only via query filter
        var orders    = await db.Orders.Include(o => o.Employee).ToListAsync();

        var grouped = employees
            .GroupBy(e => e.HousingName ?? "(No Housing)")
            .Select(g =>
            {
                var iqamaNos     = g.Select(e => e.IqamaNo).ToHashSet();
                var empOrders    = orders.Where(o => iqamaNos.Contains(o.IqamaNo)).ToList();
                var todayOrders  = empOrders.Count(o => o.StartedAt.Date == today);

                var empReports = g.Select(emp =>
                {
                    var eo    = empOrders.Where(o => o.IqamaNo == emp.IqamaNo).ToList();
                    var total = eo.Where(o => !o.IsOrder && o.EndedAt.HasValue)
                                  .Sum(o => (o.EndedAt!.Value - o.StartedAt).TotalMinutes);
                    return new EmployeeOrderCountReport(
                        IqamaNo:            emp.IqamaNo,
                        NameAR:             emp.NameAR,
                        NameEN:             emp.NameEN,
                        HousingName:        emp.HousingName,
                        RiderId:            emp.RiderId,
                        TotalOrders:        eo.Count,
                        ActiveOrders:       eo.Count(o => o.IsOrder),
                        LastOrderStartedAt: eo.Count > 0 ? eo.Max(o => o.StartedAt) : null,
                        TotalMinutesWorked: Math.Round(total, 1));
                }).ToList();

                return new HousingReport(
                    HousingName:      g.Key,
                    EmployeeCount:    g.Count(),
                    ActiveOrders:     empOrders.Count(o => o.IsOrder),
                    TotalOrdersToday: todayOrders,
                    Employees:        empReports);
            })
            .OrderBy(r => r.HousingName)
            .ToList();

        return Result.Success<IEnumerable<HousingReport>>(grouped);
    }

    public async Task<Result<IEnumerable<ServiceReport>>> GetServiceReportAsync(
        DateTime? from, DateTime? to)
    {
        var query = db.Orders.AsQueryable();
        if (from.HasValue) query = query.Where(o => o.StartedAt >= from.Value);
        if (to.HasValue)   query = query.Where(o => o.StartedAt <= to.Value);

        var orders = await query.ToListAsync();

        var report = orders
            .GroupBy(o => o.Service)
            .Select(g =>
            {
                var completed = g.Where(o => !o.IsOrder && o.EndedAt.HasValue).ToList();
                var total     = completed.Sum(o => (o.EndedAt!.Value - o.StartedAt).TotalMinutes);
                var avg       = completed.Count == 0 ? 0 : total / completed.Count;

                return new ServiceReport(
                    Service:            g.Key,
                    TotalOrders:        g.Count(),
                    ActiveOrders:       g.Count(o => o.IsOrder),
                    AvgDurationMinutes: Math.Round(avg, 1),
                    TotalMinutes:       Math.Round(total, 1));
            })
            .OrderByDescending(r => r.TotalOrders)
            .ToList();

        return Result.Success<IEnumerable<ServiceReport>>(report);
    }

    public async Task<Result<IEnumerable<DailyOrderReport>>> GetDailyReportAsync(
        DateTime from, DateTime to)
    {
        var orders = await db.Orders
            .Where(o => o.StartedAt.Date >= from.Date && o.StartedAt.Date <= to.Date)
            .ToListAsync();

        var report = orders
            .GroupBy(o => DateOnly.FromDateTime(o.StartedAt))
            .Select(g =>
            {
                var totalMinutes    = g
                    .Where(o => !o.IsOrder && o.EndedAt.HasValue)
                    .Sum(o => (o.EndedAt!.Value - o.StartedAt).TotalMinutes);
                var uniqueEmployees = g.Select(o => o.IqamaNo).Distinct().Count();
                var avgOrders       = uniqueEmployees == 0 ? 0 : (double)g.Count() / uniqueEmployees;

                return new DailyOrderReport(
                    Date:                  g.Key,
                    TotalOrders:           g.Count(),
                    UniqueEmployees:       uniqueEmployees,
                    TotalMinutesWorked:    Math.Round(totalMinutes, 1),
                    AvgOrdersPerEmployee:  Math.Round(avgOrders, 2));
            })
            .OrderBy(r => r.Date)
            .ToList();

        return Result.Success<IEnumerable<DailyOrderReport>>(report);
    }

    public async Task<Result<IEnumerable<SupervisorActivityReport>>> GetSupervisorActivityAsync(
        DateTime? from, DateTime? to)
    {
        var query = db.Orders.AsQueryable();
        if (from.HasValue) query = query.Where(o => o.StartedAt >= from.Value);
        if (to.HasValue)   query = query.Where(o => o.StartedAt <= to.Value);

        var orders = await query.ToListAsync();

        var report = orders
            .GroupBy(o => o.RequestedBy)
            .Select(g => new SupervisorActivityReport(
                SupervisorUsername: g.Key,
                OrdersCreated:      g.Count(),
                ActiveOrders:       g.Count(o => o.IsOrder),
                LastActivity:       g.Max(o => (DateTime?)o.StartedAt)))
            .OrderByDescending(r => r.OrdersCreated)
            .ToList();

        return Result.Success<IEnumerable<SupervisorActivityReport>>(report);
    }

    public async Task<Result<IEnumerable<Contracts.Order.OrderResponse>>> GetActiveOrdersAsync()
    {
        var orders = await db.Orders
            .Where(o => o.IsOrder)
            .Include(o => o.Employee)
            .OrderBy(o => o.StartedAt)
            .ToListAsync();

        return Result.Success<IEnumerable<Contracts.Order.OrderResponse>>(
            orders.Select(o => MapOrder(o)));
    }

    public async Task<Result<IEnumerable<Contracts.Order.OrderResponse>>> GetOrdersWithNullEndAsync(DateTime? date)
    {
        // Orders that ended (IsOrder = false) but EndedAt is null → day ended without follow-up
        var query = db.Orders
            .Where(o => !o.IsOrder && o.EndedAt == null)
            .Include(o => o.Employee)
            .AsQueryable();

        if (date.HasValue)
            query = query.Where(o => o.StartedAt.Date == date.Value.Date);

        var orders = await query.OrderByDescending(o => o.StartedAt).ToListAsync();

        return Result.Success<IEnumerable<Contracts.Order.OrderResponse>>(
            orders.Select(MapOrder));
    }

    private static Contracts.Order.OrderResponse MapOrder(Domain.Order o) =>
        new(Id:             o.Id,
            IqamaNo:        o.IqamaNo,
            EmployeeNameAR: o.Employee.NameAR,
            EmployeeNameEN: o.Employee.NameEN,
            HousingName:    o.Employee.HousingName,
            IsOrder:        o.IsOrder,
            Service:        o.Service,
            StartedAt:      o.StartedAt,
            EndedAt:        o.EndedAt,
            Duration:       o.EndedAt.HasValue ? o.EndedAt.Value - o.StartedAt : null,
            RequestedBy:    o.RequestedBy,
            Notes:          o.Notes,
            CreatedAt:      o.CreatedAt);
}
