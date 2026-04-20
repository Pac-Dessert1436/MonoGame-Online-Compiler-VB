using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace webapp.Models;

public sealed class CompilationSession
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
    [JsonIgnore]
    public GameProject GameProject { get; set; } = null!;
}