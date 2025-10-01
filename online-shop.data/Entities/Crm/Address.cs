namespace ECommerce.Data.Entities.Crm;

public class Address
{
    public Guid AddressId { get; set; }
    public Guid CustomerId { get; set; }
    public string CountryCode { get; set; } = null!; // char(2)
    public string? Region { get; set; }
    public string City { get; set; } = null!;
    public string Street { get; set; } = null!;
    public string? Zip { get; set; }
    public bool IsDefault { get; set; }

    public Customer Customer { get; set; } = null!;
}
