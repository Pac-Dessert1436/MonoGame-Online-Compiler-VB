namespace webapp.Models;

public sealed class ContentBuildResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Output { get; set; }
}