using System.ComponentModel.DataAnnotations;

namespace SacksAIPlatform.DataLayer.Entities;

public class Supplier
{
    [Key]
    public int SupplierID { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string Type { get; set; } = string.Empty; // Distributor, Retailer, Ingredient Supplier
    
    [MaxLength(100)]
    public string Country { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string ContactInfo { get; set; } = string.Empty;
    
    /// <summary>
    /// Collection of file configurations associated with this supplier
    /// </summary>
    public virtual ICollection<FileConfigurationHolder> FileConfigurations { get; set; } = new List<FileConfigurationHolder>();
}
