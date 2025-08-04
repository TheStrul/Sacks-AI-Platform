namespace SacksAIPlatform.DataLayer.Entities;

public class Supplier
{
    public int SupplierID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Distributor, Retailer, Ingredient Supplier
    public string Country { get; set; } = string.Empty;
    public string ContactInfo { get; set; } = string.Empty;
}
