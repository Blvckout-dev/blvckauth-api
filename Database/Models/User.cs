using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Bl4ckout.MyMasternode.Auth.Database.Models;

public class User
{
    public int Id { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int RoleId { get; set; } = 1;
    public Role? Role { get; set; }
    public ICollection<Scope>? Scopes { get; set; }
}