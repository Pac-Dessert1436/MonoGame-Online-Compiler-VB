using System.ComponentModel.DataAnnotations;

namespace webapp.Models;

public class User
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    
    public ICollection<GameProject> GameProjects { get; set; } = [];
}

public class GameProject
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

public class GameAsset
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    public string FilePath { get; set; } = string.Empty;
    
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    
    public int GameProjectId { get; set; }
    public GameProject GameProject { get; set; } = null!;
}

public class CompilationSession
{
    public int Id { get; set; }
    
    [Required]
    public string SessionId { get; set; } = string.Empty;
    
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Output { get; set; }
    
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    
    public string? CompiledGamePath { get; set; }
    
    public int GameProjectId { get; set; }
    public GameProject GameProject { get; set; } = null!;
}