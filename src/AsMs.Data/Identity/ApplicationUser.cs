using Microsoft.AspNetCore.Identity;

namespace AsMs.Data.Identity;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
}
