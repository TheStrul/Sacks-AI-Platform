using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SacksAIPlatform.DataLayer.Entities;

/// <summary>
/// Entity to store FileConfiguration objects as JSON strings with metadata
/// Each supplier can have multiple file configurations for different file formats
/// </summary>
public class FileConfigurationHolder
{
    /// <summary>
    /// Primary key
    /// </summary>
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// Name/description of this file configuration
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Foreign key to the Supplier entity
    /// Each supplier can have multiple file configurations
    /// </summary>
    [Required]
    public int SupplierId { get; set; }
    
    /// <summary>
    /// Navigation property to the associated Supplier
    /// </summary>
    [ForeignKey(nameof(SupplierId))]
    public virtual Supplier Supplier { get; set; } = null!;
    
    /// <summary>
    /// Pattern to match file names (can include wildcards like *.csv, *stock*.xlsx, etc.)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string FileNamePattern { get; set; } = string.Empty;
    
    /// <summary>
    /// File extension (e.g., .csv, .xlsx, .xls)
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string FileExtension { get; set; } = string.Empty;
    
    /// <summary>
    /// FileConfiguration object serialized as JSON string
    /// Contains all the mapping and parsing configuration
    /// </summary>
    [Required]
    [Column(TypeName = "LONGTEXT")]
    public string ConfigurationJson { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional remarks/notes about this configuration
    /// </summary>
    [MaxLength(500)]
    public string? Remarks { get; set; }
    
    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last modification timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
