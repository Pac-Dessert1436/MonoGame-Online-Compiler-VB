using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace webapp.Pages;

public class ProjectManagerModel(IHttpClientFactory httpClientFactory, ILogger<ProjectManagerModel> logger) : PageModel
{
    [BindProperty]
    public string ProjectName { get; set; } = string.Empty;

    [BindProperty]
    public string InitialCode { get; set; } = string.Empty;

    [BindProperty]
    public int DeleteProjectId { get; set; }

    public string Username { get; set; } = string.Empty;
    public int UserId { get; set; }
    public List<GameProject>? Projects { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToPage("/UserLogin");
        }

        UserId = userId.Value;
        Username = HttpContext.Session.GetString("Username") ?? "User";

        await LoadProjectsAsync();
        return Page();
    }

    private async Task LoadProjectsAsync()
    {
        try
        {
            var httpClient = httpClientFactory.CreateClient("LocalClient");
            var response = await httpClient.GetAsync($"api/project/list?userId={UserId}");

            if (response.IsSuccessStatusCode)
            {
                Projects = await response.Content.ReadFromJsonAsync<List<GameProject>>();
            }
            else
            {
                logger.LogError("Failed to load projects for user {UserId}", UserId);
                Projects = [];
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading projects");
            Projects = [];
        }
    }

    public async Task<IActionResult> OnPostCreateProjectAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToPage("/UserLogin");
        }

        UserId = userId.Value;
        Username = HttpContext.Session.GetString("Username") ?? "User";

        if (string.IsNullOrWhiteSpace(ProjectName))
        {
            ErrorMessage = "Project name is required";
            await LoadProjectsAsync();
            return Page();
        }

        try
        {
            var httpClient = httpClientFactory.CreateClient("LocalClient");
            var response = await httpClient.PostAsJsonAsync("api/project/create", new 
            {
                UserId, 
                Name = ProjectName, 
                VbCode = string.IsNullOrWhiteSpace(InitialCode) ? VbCodeEditorModel.ScaffoldCode : InitialCode 
            });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<GameProject>();
                if (result != null)
                {
                    SuccessMessage = $"Project '{ProjectName}' created successfully!";
                    logger.LogInformation("Project {ProjectName} created for user {UserId}", ProjectName, UserId);
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ErrorMessage = $"Failed to create project: {errorContent}";
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating project");
            ErrorMessage = "An error occurred while creating the project. Please try again.";
        }

        await LoadProjectsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteProjectAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToPage("/UserLogin");
        }

        UserId = userId.Value;
        Username = HttpContext.Session.GetString("Username") ?? "User";

        if (DeleteProjectId <= 0)
        {
            ErrorMessage = "Invalid project ID";
            await LoadProjectsAsync();
            return Page();
        }

        try
        {
            var httpClient = httpClientFactory.CreateClient("LocalClient");
            var response = await httpClient.DeleteAsync($"api/project/{DeleteProjectId}?userId={UserId}");

            if (response.IsSuccessStatusCode)
            {
                SuccessMessage = "Project deleted successfully!";
                logger.LogInformation("Project {ProjectId} deleted by user {UserId}", DeleteProjectId, UserId);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ErrorMessage = $"Failed to delete project: {errorContent}";
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting project");
            ErrorMessage = "An error occurred while deleting the project. Please try again.";
        }

        await LoadProjectsAsync();
        return Page();
    }
}

public class GameProject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string VbCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int UserId { get; set; }
    public List<GameAsset> Assets { get; set; } = [];
}

public class GameAsset
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
}