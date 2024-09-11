using System.ComponentModel.DataAnnotations;

namespace Blvckout.MyMasternode.Auth.Settings;

public class JwtSettings 
{
    public const string SECTION = "Jwt";
    
    [Required(AllowEmptyStrings = false)]
    public string Audience { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Issuer { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Key { get; set; } = string.Empty;
}