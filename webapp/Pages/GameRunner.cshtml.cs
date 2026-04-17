using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace webapp.Pages;

public class GameRunnerModel(IHttpClientFactory httpClientFactory) : PageModel
{
    public string GameId { get; set; } = string.Empty;
    public string GameUrl { get; set; } = string.Empty;
    public string? CompilationError { get; set; }
    public string? CompilationOutput { get; set; }
    public string GameStatus { get; set; } = "Loading";

    public async Task<IActionResult> OnGetAsync(string gameId)
    {
        if (string.IsNullOrEmpty(gameId))
        {
            return RedirectToPage("/VbCodeEditor");
        }

        GameId = gameId;
        GameUrl = $"/games/{gameId}/index.html";

        // Check game status
        try
        {
            var httpClient = httpClientFactory.CreateClient("LocalClient");
            var response = await httpClient.GetAsync($"api/monogame/status/{gameId}");
            
            if (response.IsSuccessStatusCode)
            {
                var statusResult = await response.Content.ReadFromJsonAsync<GameStatusResult>();
                if (statusResult != null)
                {
                    GameStatus = statusResult.Status;
                    if (statusResult.Status == "NotFound")
                    {
                        CompilationError = "Game not found. It may have expired or been removed.";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            CompilationError = $"Error checking game status: {ex.Message}";
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