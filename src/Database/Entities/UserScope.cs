namespace Blvckout.BlvckAuth.API.Database.Entities;

public class UserScope
{
    public int UserId { get; set; }
    public int ScopeId { get; set; }
    public User? User { get; set; }
    public Scope? Scope { get; set; }
}