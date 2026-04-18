using System.ComponentModel.DataAnnotations;

namespace webapp.Models;

public sealed class GameProject
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public string VbCode { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    public ICollection<GameAsset> Assets { get; set; } = [];
    public ICollection<CompilationSession> CompilationSessions { get; set; } = [];
}
