namespace EmployeeOrderApi.Domain;

public sealed class Order
{
    public int      Id          { get; set; }
    public long     IqamaNo     { get; set; }           // FK → Employee.IqamaNo
    public bool     IsOrder     { get; set; }           // true = currently active
    public string   Service     { get; set; } = null!;  // service / task description
    public DateTime StartedAt   { get; set; }
    public DateTime? EndedAt    { get; set; }           // null = active or day ended with no new order

    // ── Audit ─────────────────────────────────────────────────────────────────
    public string   RequestedBy { get; set; } = null!;  // supervisor username
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow.AddHours(3);
    public string?  Notes       { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public Employee Employee { get; set; } = null!;
}
