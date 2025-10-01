using System.Net;

namespace ECommerce.Data.Entities.Crm;

public enum Gender { M, F, U }

public class Customer
{
    public Guid CustomerId { get; set; }
    public Guid? UserId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public Gender Gender { get; set; } = Gender.U;
    public DateOnly? BirthDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Auth.User? User { get; set; }
    public ICollection<Address> Addresses { get; set; } = new List<Address>();
    public ICollection<Sales.Cart> Carts { get; set; } = new List<Sales.Cart>();
}
