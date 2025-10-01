namespace ECommerce.Data.Entities.Auth;

public class User
{
    public Guid UserId { get; set; }
    public string? PhoneE164 { get; set; }
    public string PasswordHash { get; set; } = null!;
    public bool IsEmailVerified { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // navigation
    public Crm.Customer? Customer { get; set; }
}
