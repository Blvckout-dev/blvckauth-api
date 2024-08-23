using System.ComponentModel.DataAnnotations;

namespace Bl4ckout.MyMasternode.Auth.Settings;

public class JwtSettings 
{
    public const string SECTION = "Jwt";

    [Required(AllowEmptyStrings = false)]
    public string Key { get; set; } = string.Empty;
}