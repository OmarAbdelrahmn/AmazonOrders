namespace EmployeeOrderApi.Contracts.Employee;

// ── Requests ──────────────────────────────────────────────────────────────────
public record CreateEmployeeRequest(
    string  NameAR,
    string  NameEN,
    long    IqamaNo,
    string? RiderId,
    string? HousingName);

public record UpdateEmployeeRequest(
    string? NameAR,
    string? NameEN,
    string? RiderId,
    string? HousingName);

public record EmployeeSearchFilter(
    string?  NameAR      = null,
    string?  NameEN      = null,
    string?  HousingName = null,
    string?  RiderId     = null,
    bool?    IsDeleted   = false,
    int      Page        = 1,
    int      PageSize    = 20);

// ── Responses ─────────────────────────────────────────────────────────────────
public record EmployeeResponse(
    int      Id,
    string   NameAR,
    string   NameEN,
    long     IqamaNo,
    string?  ImgUrl,
    string?  RiderId,
    string?  HousingName,
    bool     IsDeleted,
    bool     IsUpdated,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string?  CreatedBy,
    string?  UpdatedBy,
    bool     HasActiveOrder);

public record EmployeeDetailResponse(
    int      Id,
    string   NameAR,
    string   NameEN,
    long     IqamaNo,
    string?  ImgUrl,
    string?  RiderId,
    string?  HousingName,
    bool     IsDeleted,
    bool     IsUpdated,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? DeletedAt,
    string?  CreatedBy,
    string?  UpdatedBy,
    string?  DeletedBy,
    OrderSummary? ActiveOrder,
    int      TotalOrders);

public record OrderSummary(
    int      Id,
    string   Service,
    DateTime StartedAt,
    DateTime? EndedAt,
    string   RequestedBy);

public record PagedList<T>(
    List<T> Data,
    int     TotalCount,
    int     Page,
    int     PageSize,
    int     TotalPages);
