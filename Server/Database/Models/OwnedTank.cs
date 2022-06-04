using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VoxelTanksServer.Database.Models; 

public class OwnedTank {
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }
    
    public int PlayerId { get; set; }
    [ForeignKey("PlayerId")]
    public PlayerData Player { get; set; }   
}