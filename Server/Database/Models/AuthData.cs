using System.ComponentModel.DataAnnotations;

namespace VoxelTanksServer.Database.Models;

public class AuthData {
    [Key] public int Id { get; set; }
    public string Login { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public int Email_Confirm { get; set; }
    public string Ip { get; set; }
    public string AuthId { get; set; }
}