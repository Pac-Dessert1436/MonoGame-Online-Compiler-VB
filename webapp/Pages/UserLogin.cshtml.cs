using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace webapp.Pages;

public partial class UserLoginModel(IHttpClientFactory httpClientFactory, ILogger<UserLoginModel> logger) : PageModel
{
    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostLoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Email and password are required";
            return Page();
        }

        try
        {
            var httpClient = httpClientFactory.CreateClient("LocalClient");
            var response = await httpClient.PostAsJsonAsync("api/auth/login", new { Email, Password });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthResult>();
                if (result?.Success == true)
                {
                    HttpContext.Session.SetInt32("UserId", result.UserId);
                    HttpContext.Session.SetString("Username", result.Username);
                    logger.LogInformation("User {Username} logged in successfully", result.Username);
                    return RedirectToPage("/ProjectManager");
                }
                else
                {
                    ErrorMessage = result?.Message ?? "Login failed";
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ErrorMessage = $"Login failed: {errorContent}";
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during login");
            ErrorMessage = "An error occurred during login. Please try again.";
        }

        return Page();
    }
}

public class AuthResult
{
    public bool Success { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}