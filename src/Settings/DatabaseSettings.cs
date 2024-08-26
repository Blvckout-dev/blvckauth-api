using System.ComponentModel.DataAnnotations;

namespace Bl4ckout.MyMasternode.Auth.Settings;

public class DatabaseSettings
{
    public const string SECTION = "Database";

    [Required(AllowEmptyStrings = false)]
    public string? ConnectionString { get; set; }
    
    public bool SeedData { get; set; } = true;
}