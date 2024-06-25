namespace Bl4ckout.MyMasternode.Auth.Database.Models;

public class Scope
{
    public int Id { get; set; }
    public string? Name { get; set; }
    
    public ICollection<User>? Users { get; set; }
}