using System.Diagnostics;

namespace ECommerce.Data.Entities.Pricing;

public class PriceList
{
    public Guid PriceListId { get; set; }
    public string Code { get; set; } = null!;
    public string Currency { get; set; } = null!; // char(3)
    public bool IsActive { get; set; }

    public ICollection<Price> Prices { get; set; } = new List<Price>();
}
