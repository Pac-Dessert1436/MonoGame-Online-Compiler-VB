using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace webapp.Pages;

public partial class UserRegisterModel(IHttpClientFactory httpClientFactory, ILogger<UserRegisterModel> logger) : PageModel
{
    //private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ILogger<UserRegisterModel> _logger = logger;

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostRegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Email) || 
            string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(ConfirmPassword))
        {
            ErrorMessage = "All fields are required";
            return Page();
        }

        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match";
            return Page();
        }

        if (Password.Length < 6)
        {
            ErrorMessage = "Password must be at least 6 characters long";
            return Page();
        }

        try
        {
            var httpClient = httpClientFactory.CreateClient("LocalClient");
            var response = await httpClient.PostAsJsonAsync("api/auth/register", new { Username, Email, Password });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthResult>();
                if (result?.Success == true)
                {
                    SuccessMessage = "Registration successful! Please login.";
                    _logger.LogInformation("New user {Username} registered successfully", Username);
                    return RedirectToPage("/UserLogin", new { message = "Registration successful! Please login." });
                }
                else
                {
                    ErrorMessage = result?.Message ?? "Registration failed";
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ErrorMessage = $"Registration failed: {errorContent}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            ErrorMessage = "An error occurred during registration. Please try again.";
        }

        return Page();
    }
}