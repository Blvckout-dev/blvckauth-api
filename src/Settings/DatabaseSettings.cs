using System.ComponentModel.DataAnnotations;

namespace Blvckout.BlvckAuth.Settings;

public class DatabaseSettings
{
    public const string SECTION = "Database";

    [Required(AllowEmptyStrings = false)]
    public string? ConnectionString { get; set; }
    
    public bool SeedData { get; set; } = false;
}