namespace Bl4ckout.MyMasternode.Auth.Database.Models;

public class Scope
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    public ICollection<User>? Users { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is Scope other)
            return Id == other.Id && Name == other.Name;

        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Name);
    }
}