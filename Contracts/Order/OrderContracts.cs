namespace EmployeeOrderApi.Contracts.Order;

// ── Requests ──────────────────────────────────────────────────────────────────
public record CreateOrderRequest(
    long    IqamaNo,
    string  Service,
    string? Notes);

public record OrderFilterRequest(
    long?    IqamaNo     = null,
    bool?    IsOrder     = null,
    string?  Service     = null,
    DateTime? DateFrom   = null,
    DateTime? DateTo     = null,
    string?  RequestedBy = null,
    int      Page        = 1,
    int      PageSize    = 20);

// ── Responses ─────────────────────────────────────────────────────────────────
public record OrderResponse(
    int       Id,
    long      IqamaNo,
    string    EmployeeNameAR,
    string    EmployeeNameEN,
    string?   HousingName,
    bool      IsOrder,
    string    Service,
    DateTime  StartedAt,
    DateTime? EndedAt,
    TimeSpan? Duration,
    string    RequestedBy,
    string?   Notes,
    DateTime  CreatedAt);

public record ActiveOrderResponse(
    int      Id,
    long     IqamaNo,
    string   EmployeeNameAR,
    string   EmployeeNameEN,
    string?  HousingName,
    string   Service,
    DateTime StartedAt,
    double   ElapsedMinutes,
    string   RequestedBy,
    string?  Notes);
