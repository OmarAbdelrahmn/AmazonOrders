namespace EmployeeOrderApi.Domain;

public sealed class Employee
{
    public int     Id          { get; set; }
    public string  NameAR      { get; set; } = null!;
    public string  NameEN      { get; set; } = null!;
    public long    IqamaNo     { get; set; }           // unique identifier
    public string? ImgUrl      { get; set; }           // relative path under wwwroot/images
    public string? RiderId     { get; set; }
    public string? HousingName { get; set; }

    // ── Audit ─────────────────────────────────────────────────────────────────
    public bool      IsDeleted  { get; set; }
    public bool      IsUpdated  { get; set; }
    public DateTime  CreatedAt  { get; set; } = DateTime.UtcNow.AddHours(3);
    public DateTime? UpdatedAt  { get; set; }
    public DateTime? DeletedAt  { get; set; }
    public string?   CreatedBy  { get; set; }
    public string?   UpdatedBy  { get; set; }
    public string?   DeletedBy  { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public ICollection<Order> Orders { get; set; } = [];
}
