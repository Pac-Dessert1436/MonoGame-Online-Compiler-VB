using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace webapp.Pages;

public class ProjectManagerModel : PageModel
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

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProjectManagerModel> _logger;

    public ProjectManagerModel(IHttpClientFactory httpClientFactory, ILogger<ProjectManagerModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

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
            var httpClient = _httpClientFactory.CreateClient("LocalClient");
            var response = await httpClient.GetAsync($"api/project/list?userId={UserId}");

            if (response.IsSuccessStatusCode)
            {
                Projects = await response.Content.ReadFromJsonAsync<List<GameProject>>();
            }
            else
            {
                _logger.LogError("Failed to load projects for user {UserId}", UserId);
                Projects = new List<GameProject>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading projects");
            Projects = new List<GameProject>();
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
            var httpClient = _httpClientFactory.CreateClient("LocalClient");
            var response = await httpClient.PostAsJsonAsync("api/project/create", new 
            { 
                UserId = UserId, 
                Name = ProjectName, 
                VbCode = string.IsNullOrWhiteSpace(InitialCode) ? ScaffoldCode : InitialCode 
            });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<GameProject>();
                if (result != null)
                {
                    SuccessMessage = $"Project '{ProjectName}' created successfully!";
                    _logger.LogInformation("Project {ProjectName} created for user {UserId}", ProjectName, UserId);
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
            _logger.LogError(ex, "Error creating project");
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
            var httpClient = _httpClientFactory.CreateClient("LocalClient");
            var response = await httpClient.DeleteAsync($"api/project/{DeleteProjectId}?userId={UserId}");

            if (response.IsSuccessStatusCode)
            {
                SuccessMessage = "Project deleted successfully!";
                _logger.LogInformation("Project {ProjectId} deleted by user {UserId}", DeleteProjectId, UserId);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ErrorMessage = $"Failed to delete project: {errorContent}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting project");
            ErrorMessage = "An error occurred while deleting the project. Please try again.";
        }

        await LoadProjectsAsync();
        return Page();
    }

    internal const string ScaffoldCode = @"
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input

Public Class GameMain
    Inherits Game

    Private ReadOnly _graphics As GraphicsDeviceManager
    Private _spriteBatch As SpriteBatch
    Private _position As Vector2 = New Vector2(100.0F, 100.0F)
    Private _speed As Single = 200.0F

    Public Sub New()
        _graphics = New GraphicsDeviceManager(Me)
        Content.RootDirectory = ""Content""
        IsMouseVisible = True
    End Sub

    Protected Overrides Sub LoadContent()
        _spriteBatch = New SpriteBatch(GraphicsDevice)
    End Sub

    Protected Overrides Sub Update(gameTime As GameTime)
        Dim keyboard As KeyboardState = Keyboard.GetState()
        Dim deltaTime As Single = CSng(gameTime.ElapsedGameTime.TotalSeconds)

        If keyboard.IsKeyDown(Keys.W) Then
            _position.Y -= _speed * deltaTime
        End If
        If keyboard.IsKeyDown(Keys.S) Then
            _position.Y += _speed * deltaTime
        End If
        If keyboard.IsKeyDown(Keys.A) Then
            _position.X -= _speed * deltaTime
        End If
        If keyboard.IsKeyDown(Keys.D) Then
            _position.X += _speed * deltaTime
        End If

        MyBase.Update(gameTime)
    End Sub

    Protected Overrides Sub Draw(gameTime As GameTime)
        GraphicsDevice.Clear(Color.CornflowerBlue)

        _spriteBatch.Begin()
        ' Draw your game elements here
        _spriteBatch.End()

        MyBase.Draw(gameTime)
    End Sub
End Class";
}

public class GameProject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string VbCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int UserId { get; set; }
    public List<GameAsset> Assets { get; set; } = new List<GameAsset>();
}

public class GameAsset
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
}