using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using webapp.Models;

namespace webapp.Pages;

public class GameRunnerModel : PageModel
{
    public string GameId { get; set; } = string.Empty;
    public string GameUrl { get; set; } = string.Empty;
    public string? CompilationError { get; set; }
    public string? CompilationOutput { get; set; }
    public string GameStatus { get; set; } = "Loading";

    private readonly IHttpClientFactory _httpClientFactory;

    public GameRunnerModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> OnGetAsync(string gameId)
    {
        if (string.IsNullOrEmpty(gameId))
        {
            return RedirectToPage("/VbCodeEditor");
        }

        GameId = gameId;
        GameUrl = $"/games/{gameId}/index.html";

        // Extract sessionId from gameId (format: game_{userId}_{projectId}_{sessionId})
        var parts = gameId.Split('_');
        if (parts.Length >= 4)
        {
            var sessionId = string.Join('_', parts.Skip(3));
            
            // Check compilation session status
            try
            {
                var httpClient = _httpClientFactory.CreateClient("LocalClient");
                var response = await httpClient.GetAsync($"api/project/session/{sessionId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var session = await response.Content.ReadFromJsonAsync<CompilationSession>();
                    if (session != null)
                    {
                        if (session.CompletedAt.HasValue)
                        {
                            if (session.Success)
                            {
                                GameStatus = "Ready";
                            }
                            else
                            {
                                GameStatus = "Failed";
                                CompilationError = session.ErrorMessage ?? "Compilation failed";
                                CompilationOutput = session.Output;
                            }
                        }
                        else
                        {
                            GameStatus = "Compiling";
                        }
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // Session not found, check if game files exist
                    var gamePath = Path.Combine(Directory.GetCurrentDirectory(), "CompiledGames", gameId);
                    if (Directory.Exists(gamePath))
                    {
                        var indexFile = Path.Combine(gamePath, "index.html");
                        if (System.IO.File.Exists(indexFile))
                        {
                            GameStatus = "Ready";
                        }
                        else
                        {
                            GameStatus = "Compiling";
                        }
                    }
                    else
                    {
                        GameStatus = "NotFound";
                        CompilationError = "Game not found. It may have expired or been removed.";
                    }
                }
            }
            catch (Exception ex)
            {
                CompilationError = $"Error checking game status: {ex.Message}";
                GameStatus = "Error";
            }
        }
        else
        {
            // Fallback to old behavior for legacy gameIds
            var gamePath = Path.Combine(Directory.GetCurrentDirectory(), "CompiledGames", gameId);
            if (Directory.Exists(gamePath))
            {
                var indexFile = Path.Combine(gamePath, "index.html");
                if (System.IO.File.Exists(indexFile))
                {
                    GameStatus = "Ready";
                }
                else
                {
                    GameStatus = "Compiling";
                }
            }
            else
            {
                GameStatus = "NotFound";
                CompilationError = "Game not found. It may have expired or been removed.";
            }
        }

        return Page();
    }
}

public class GameStatusResult
{
    public string GameId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? GameUrl { get; set; }
}