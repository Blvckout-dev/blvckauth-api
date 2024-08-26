namespace Bl4ckout.MyMasternode.Auth.Database.Models;

public class UserScope
{
    public int UserId { get; set; }
    public int ScopeId { get; set; }
    public User? User { get; set; }
    public Scope? Scope { get; set; }
}