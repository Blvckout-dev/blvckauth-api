namespace Bl4ckout.MyMasternode.Auth.Models;

public record AuthenticationToken(bool Success, string? Token, string? ErrorMsg);