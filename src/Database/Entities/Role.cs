namespace Blvckout.BlvckAuth.API.Database.Entities;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    
    public ICollection<User>? Users { get; set; }
}