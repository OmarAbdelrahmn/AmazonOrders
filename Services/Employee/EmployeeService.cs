using EmployeeOrderApi.Contracts.Employee;
using EmployeeOrderApi.Data;
using EmployeeOrderApi.Services.Common;
using EmployeeOrderApi.Services.Image;
using Microsoft.EntityFrameworkCore;

namespace EmployeeOrderApi.Services.Employee;

// ── Interface ─────────────────────────────────────────────────────────────────
public interface IEmployeeService
{
    Task<Result<EmployeeResponse>>            CreateAsync(CreateEmployeeRequest request, IFormFile? image, string createdBy);
    Task<Result<EmployeeResponse>>            UpdateAsync(long iqamaNo, UpdateEmployeeRequest request, IFormFile? image, string updatedBy);
    Task<Result>                              DeleteAsync(long iqamaNo, string deletedBy);
    Task<Result>                              RestoreAsync(long iqamaNo);
    Task<Result<EmployeeDetailResponse>>      GetByIqamaAsync(long iqamaNo);
    Task<Result<PagedList<EmployeeResponse>>> GetAllAsync(EmployeeSearchFilter filter);
    Task<Result<IEnumerable<EmployeeResponse>>> SearchAsync(string keyword);
}

// ── Implementation ────────────────────────────────────────────────────────────
public sealed class EmployeeService(AppDbContext db, IImageService imageService) : IEmployeeService
{
    public async Task<Result<EmployeeResponse>> CreateAsync(
        CreateEmployeeRequest request, IFormFile? image, string createdBy)
    {
        // Check duplicate IqamaNo (bypass soft-delete filter with IgnoreQueryFilters)
        var exists = await db.Employees
            .IgnoreQueryFilters()
            .AnyAsync(e => e.IqamaNo == request.IqamaNo);

        if (exists)
            return Result.Failure<EmployeeResponse>(
                new Error("Employee.Duplicate", $"An employee with IqamaNo {request.IqamaNo} already exists.", 409));

        string? imgUrl = null;
        if (image is not null)
        {
            try   { imgUrl = await imageService.SaveImageAsync(image); }
            catch (Exception ex)
            { return Result.Failure<EmployeeResponse>(new Error("Image.Invalid", ex.Message, 400)); }
        }

        var employee = new Domain.Employee
        {
            NameAR      = request.NameAR,
            NameEN      = request.NameEN,
            IqamaNo     = request.IqamaNo,
            ImgUrl      = imgUrl,
            RiderId     = request.RiderId,
            HousingName = request.HousingName,
            CreatedBy   = createdBy,
            CreatedAt   = DateTime.UtcNow.AddHours(3)
        };

        db.Employees.Add(employee);
        await db.SaveChangesAsync();

        return Result.Success(Map(employee, hasActiveOrder: false));
    }

    public async Task<Result<EmployeeResponse>> UpdateAsync(
        long iqamaNo, UpdateEmployeeRequest request, IFormFile? image, string updatedBy)
    {
        var employee = await db.Employees
            .FirstOrDefaultAsync(e => e.IqamaNo == iqamaNo);

        if (employee is null)
            return Result.Failure<EmployeeResponse>(
                new Error("Employee.NotFound", $"Employee with IqamaNo {iqamaNo} was not found.", 404));

        if (image is not null)
        {
            // Delete old image if present
            imageService.DeleteImage(employee.ImgUrl);
            try   { employee.ImgUrl = await imageService.SaveImageAsync(image); }
            catch (Exception ex)
            { return Result.Failure<EmployeeResponse>(new Error("Image.Invalid", ex.Message, 400)); }
        }

        if (request.NameAR      is not null) employee.NameAR      = request.NameAR;
        if (request.NameEN      is not null) employee.NameEN      = request.NameEN;
        if (request.RiderId     is not null) employee.RiderId     = request.RiderId;
        if (request.HousingName is not null) employee.HousingName = request.HousingName;

        employee.IsUpdated = true;
        employee.UpdatedAt = DateTime.UtcNow.AddHours(3);
        employee.UpdatedBy = updatedBy;

        await db.SaveChangesAsync();

        var hasActive = await db.Orders
            .AnyAsync(o => o.IqamaNo == iqamaNo && o.IsOrder);

        return Result.Success(Map(employee, hasActive));
    }

    public async Task<Result> DeleteAsync(long iqamaNo, string deletedBy)
    {
        var employee = await db.Employees
            .FirstOrDefaultAsync(e => e.IqamaNo == iqamaNo);

        if (employee is null)
            return Result.Failure(
                new Error("Employee.NotFound", $"Employee with IqamaNo {iqamaNo} was not found.", 404));

        employee.IsDeleted  = true;
        employee.DeletedAt  = DateTime.UtcNow.AddHours(3);
        employee.DeletedBy  = deletedBy;

        await db.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> RestoreAsync(long iqamaNo)
    {
        var employee = await db.Employees
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.IqamaNo == iqamaNo);

        if (employee is null)
            return Result.Failure(
                new Error("Employee.NotFound", $"Employee with IqamaNo {iqamaNo} was not found.", 404));

        if (!employee.IsDeleted)
            return Result.Failure(
                new Error("Employee.NotDeleted", "Employee is not deleted.", 400));

        employee.IsDeleted = false;
        employee.DeletedAt = null;
        employee.DeletedBy = null;

        await db.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<EmployeeDetailResponse>> GetByIqamaAsync(long iqamaNo)
    {
        var employee = await db.Employees
            .IgnoreQueryFilters()
            .Include(e => e.Orders)
            .FirstOrDefaultAsync(e => e.IqamaNo == iqamaNo);

        if (employee is null)
            return Result.Failure<EmployeeDetailResponse>(
                new Error("Employee.NotFound", $"Employee with IqamaNo {iqamaNo} was not found.", 404));

        var activeOrder = employee.Orders
            .FirstOrDefault(o => o.IsOrder);

        var response = new EmployeeDetailResponse(
            Id:           employee.Id,
            NameAR:       employee.NameAR,
            NameEN:       employee.NameEN,
            IqamaNo:      employee.IqamaNo,
            ImgUrl:       employee.ImgUrl,
            RiderId:      employee.RiderId,
            HousingName:  employee.HousingName,
            IsDeleted:    employee.IsDeleted,
            IsUpdated:    employee.IsUpdated,
            CreatedAt:    employee.CreatedAt,
            UpdatedAt:    employee.UpdatedAt,
            DeletedAt:    employee.DeletedAt,
            CreatedBy:    employee.CreatedBy,
            UpdatedBy:    employee.UpdatedBy,
            DeletedBy:    employee.DeletedBy,
            ActiveOrder:  activeOrder is null ? null : new OrderSummary(
                              Id:          activeOrder.Id,
                              Service:     activeOrder.Service,
                              StartedAt:   activeOrder.StartedAt,
                              EndedAt:     activeOrder.EndedAt,
                              RequestedBy: activeOrder.RequestedBy),
            TotalOrders:  employee.Orders.Count);

        return Result.Success(response);
    }

    public async Task<Result<PagedList<EmployeeResponse>>> GetAllAsync(EmployeeSearchFilter filter)
    {
        var query = db.Employees
            .IgnoreQueryFilters()
            .AsQueryable();

        // Respect deleted filter (default = false = show only active)
        query = filter.IsDeleted switch
        {
            true  => query.Where(e => e.IsDeleted),
            false => query.Where(e => !e.IsDeleted),
            null  => query   // show all
        };

        if (!string.IsNullOrWhiteSpace(filter.NameAR))
            query = query.Where(e => e.NameAR.Contains(filter.NameAR));

        if (!string.IsNullOrWhiteSpace(filter.NameEN))
            query = query.Where(e => e.NameEN.Contains(filter.NameEN));

        if (!string.IsNullOrWhiteSpace(filter.HousingName))
            query = query.Where(e => e.HousingName != null && e.HousingName.Contains(filter.HousingName));

        if (!string.IsNullOrWhiteSpace(filter.RiderId))
            query = query.Where(e => e.RiderId != null && e.RiderId.Contains(filter.RiderId));

        var total = await query.CountAsync();
        var skip  = (filter.Page - 1) * filter.PageSize;

        var employees = await query
            .OrderBy(e => e.NameEN)
            .Skip(skip)
            .Take(filter.PageSize)
            .ToListAsync();

        // Fetch IqamaNos that have active orders in one round-trip
        var iqamaNos      = employees.Select(e => e.IqamaNo).ToList();
        var activeIqamas  = await db.Orders
            .Where(o => iqamaNos.Contains(o.IqamaNo) && o.IsOrder)
            .Select(o => o.IqamaNo)
            .ToListAsync();
        var activeSet = activeIqamas.ToHashSet();

        var data = employees.Select(e => Map(e, activeSet.Contains(e.IqamaNo))).ToList();

        var paged = new PagedList<EmployeeResponse>(
            Data:       data,
            TotalCount: total,
            Page:       filter.Page,
            PageSize:   filter.PageSize,
            TotalPages: (int)Math.Ceiling(total / (double)filter.PageSize));

        return Result.Success(paged);
    }

    public async Task<Result<IEnumerable<EmployeeResponse>>> SearchAsync(string keyword)
    {
        keyword = keyword.ToLower();

        var employees = await db.Employees
            .Where(e =>
                e.NameAR.ToLower().Contains(keyword) ||
                e.NameEN.ToLower().Contains(keyword) ||
                e.IqamaNo.ToString().StartsWith(keyword) ||
                (e.RiderId     != null && e.RiderId.ToLower().Contains(keyword)) ||
                (e.HousingName != null && e.HousingName.ToLower().Contains(keyword)))
            .AsNoTracking()
            .ToListAsync();

        var iqamaNos     = employees.Select(e => e.IqamaNo).ToList();
        var activeIqamas = await db.Orders
            .Where(o => iqamaNos.Contains(o.IqamaNo) && o.IsOrder)
            .Select(o => o.IqamaNo)
            .ToListAsync();
        var activeSet = activeIqamas.ToHashSet();

        return Result.Success<IEnumerable<EmployeeResponse>>(
            employees.Select(e => Map(e, activeSet.Contains(e.IqamaNo))));
    }

    // ── Mapping ───────────────────────────────────────────────────────────────
    private static EmployeeResponse Map(Domain.Employee e, bool hasActiveOrder) => new(
        Id:            e.Id,
        NameAR:        e.NameAR,
        NameEN:        e.NameEN,
        IqamaNo:       e.IqamaNo,
        ImgUrl:        e.ImgUrl,
        RiderId:       e.RiderId,
        HousingName:   e.HousingName,
        IsDeleted:     e.IsDeleted,
        IsUpdated:     e.IsUpdated,
        CreatedAt:     e.CreatedAt,
        UpdatedAt:     e.UpdatedAt,
        CreatedBy:     e.CreatedBy,
        UpdatedBy:     e.UpdatedBy,
        HasActiveOrder: hasActiveOrder);
}
