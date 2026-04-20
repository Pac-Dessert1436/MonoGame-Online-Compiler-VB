using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace webapp.Models;
public sealed class GameAsset
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
    [JsonIgnore]
    public GameProject GameProject { get; set; } = null!;
}