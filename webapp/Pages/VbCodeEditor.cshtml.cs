using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using webapp.Controllers;

namespace webapp.Pages;

public partial class VbCodeEditorModel : PageModel
{
    [BindProperty]
    public string VbCode { get; set; } = string.Empty;

    [BindProperty]
    public int ProjectId { get; set; }

    [BindProperty]
    public string ProjectName { get; set; } = string.Empty;

    [BindProperty]
    public List<IFormFile>? AssetFiles { get; set; }

    public string? CompilationError { get; set; }
    public string? GameUrl { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public bool IsAuthenticated { get; set; }

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<VbCodeEditorModel> _logger;

    public VbCodeEditorModel(IHttpClientFactory httpClientFactory, ILogger<VbCodeEditorModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    internal const string ScaffoldCode = @"Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input

Public Enum GameState As Short
    TitleScreen = 0
    Playing = 1
    Paused = 2
    GameOver = 3
End Enum

Partial Public Module Essentials
#Region ""Events""
    Public Event GameStateChanged(newState As GameState)
    ' TODO: Add more events as needed
#End Region

    Public Function ParamsSum(ParamArray values As Single()) As Single
        Return If(values.Length = 0, 0F, values.Sum())
    End Function

    Public Function ParamsProduct(ParamArray values As Single()) As Single
        If values.Length = 0 OrElse values.Contains(0F) Then Return 0F
        If values.Length = 1 Then Return values(0)
        Return values.Aggregate(1.0F, Function(x, y) x * y)
    End Function

    ' TODO: Add more methods or properties as needed
End Module

Public Class GameMain
    Inherits Game

    Private ReadOnly _graphics As GraphicsDeviceManager
    Private _spriteBatch As SpriteBatch
    Private _gameState As GameState = GameState.TitleScreen

    Public Sub New()
        _graphics = New GraphicsDeviceManager(Me)
        Content.RootDirectory = ""Content""
        IsMouseVisible = True
    End Sub

    Private Sub OnGameStateChanged(newState As GameState)
        _gameState = newState
    End Sub

    Protected Overrides Sub Initialize()
        ' TODO: Add your initialization logic here
        AddHandler Essentials.GameStateChanged, AddressOf OnGameStateChanged

        MyBase.Initialize()
    End Sub

    Protected Overrides Sub LoadContent()
        _spriteBatch = New SpriteBatch(GraphicsDevice)

        ' TODO: use Me.Content to load your game content here
    End Sub

    Protected Overrides Sub Update(gameTime As GameTime)
        If GamePad.GetState(PlayerIndex.One).Buttons.Back = ButtonState.Pressed OrElse
            Keyboard.GetState().IsKeyDown(Keys.Escape) Then [Exit]()

        ' TODO: Add your update logic, such as game state management, input handling, etc.
        ' TODO: Also schedule any events you want to raise in the Draw method, e.g:
        '    ScheduleEvent_GameStateChanged(GameState.Playing)


        MyBase.Update(gameTime)
    End Sub

    Protected Overrides Sub Draw(gameTime As GameTime)
        GraphicsDevice.Clear(Color.CornflowerBlue)
        RaiseScheduledEvents()  ' Required for event processing

        With _spriteBatch
            .Begin(samplerState:=SamplerState.PointClamp)

            ' TODO: Add your drawing code for game elements like sprites, backgrounds, etc.

            .End()
        End With

        MyBase.Draw(gameTime)
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            RemoveHandler Essentials.GameStateChanged, AddressOf OnGameStateChanged

            ' TODO: Add more disposal logic (if any) for managed resources here
        End If

        ' TODO: Dispose any unmanaged resources here

        MyBase.Dispose(disposing)
    End Sub
End Class";

    public async Task<IActionResult> OnGetAsync(int? projectId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToPage("/UserLogin");
        }

        UserId = userId.Value;
        Username = HttpContext.Session.GetString("Username") ?? "User";
        IsAuthenticated = true;

        if (projectId.HasValue)
        {
            ProjectId = projectId.Value;
            await LoadProjectAsync();
        }
        else
        {
            VbCode = ScaffoldCode;
            ProjectName = "New Project";
        }

        _logger.LogInformation("OnGetAsync called. ProjectId: {ProjectId}, VbCode length: {Length}", ProjectId, VbCode?.Length ?? 0);
        return Page();
    }

    private async Task LoadProjectAsync()
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("LocalClient");
            var response = await httpClient.GetAsync($"api/project/{ProjectId}?userId={UserId}");

            if (response.IsSuccessStatusCode)
            {
                var project = await response.Content.ReadFromJsonAsync<GameProject>();
                if (project != null)
                {
                    VbCode = project.VbCode;
                    ProjectName = project.Name;
                }
            }
            else
            {
                _logger.LogError("Failed to load project {ProjectId} for user {UserId}", ProjectId, UserId);
                VbCode = ScaffoldCode;
                ProjectName = "Error Loading Project";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading project");
            VbCode = ScaffoldCode;
            ProjectName = "Error Loading Project";
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "VbCode length: {Length}")]
    partial void LogVbCodeLength(int length);

    [LoggerMessage(Level = LogLevel.Information, Message = "VbCode content preview: {Preview}")]
    partial void LogVbCodeContentPreview(string preview);

    public async Task<IActionResult> OnPostCompileAsync()
    {
        _logger.LogInformation("OnPostCompileAsync called");
        LogVbCodeLength(VbCode?.Length ?? 0);
        
        if (!string.IsNullOrEmpty(VbCode))
        {
            LogVbCodeContentPreview(VbCode[..Math.Min(100, VbCode.Length)]);
        }

        if (string.IsNullOrWhiteSpace(VbCode))
        {
            _logger.LogWarning("VbCode is empty or whitespace");
            ModelState.AddModelError("VbCode", "VB.NET code is required");
            return Page();
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient("LocalClient");

            // Update project with current code
            await UpdateProjectAsync();

            // Compile the project using the enhanced compilation endpoint
            var response = await httpClient.PostAsJsonAsync("api/monogame/compile-enhanced", new EnhancedCompileRequest { ProjectId = ProjectId, UserId = UserId });
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CompilationResult>();
                if (result?.Success == true)
                {
                    GameUrl = result.GameUrl;
                    return RedirectToPage("/GameRunner", new { gameId = result.GameId });
                }
                else
                {
                    CompilationError = result?.ErrorMessage ?? "Unknown compilation error";
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                CompilationError = $"Compilation failed: {errorContent}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during compilation");
            CompilationError = $"Error during compilation: {ex.Message}";
        }

        return Page();
    }

    private async Task UpdateProjectAsync()
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("LocalClient");
            await httpClient.PutAsJsonAsync($"api/project/{ProjectId}", new 
            {
                UserId, 
                Name = ProjectName,
                VbCode
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update project {ProjectId}", ProjectId);
        }
    }

    public async Task<IActionResult> OnPostCompileWithAssetsAsync()
    {
        _logger.LogInformation("OnPostCompileWithAssetsAsync called");
        LogVbCodeLength(VbCode?.Length ?? 0);

        if (string.IsNullOrWhiteSpace(VbCode))
        {
            _logger.LogWarning("VbCode is empty or whitespace in CompileWithAssets");
            ModelState.AddModelError("VbCode", "VB.NET code is required");
            return Page();
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient("LocalClient");

            // Update project with current code
            await UpdateProjectAsync();

            // Compile with new assets
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(ProjectId.ToString()), "ProjectId");
            content.Add(new StringContent(UserId.ToString()), "UserId");
            content.Add(new StringContent(VbCode), "VbCode"); // Add VbCode explicitly
            
            if (AssetFiles != null)
            {
                foreach (var file in AssetFiles)
                {
                    content.Add(new StreamContent(file.OpenReadStream()), file.FileName, file.FileName);
                }
            }

            var response = await httpClient.PostAsync("api/monogame/compile-with-new-assets", content);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CompilationResult>();
                if (result?.Success == true)
                {
                    GameUrl = result.GameUrl;
                    return RedirectToPage("/GameRunner", new { gameId = result.GameId });
                }
                else
                {
                    CompilationError = result?.ErrorMessage ?? "Unknown compilation error";
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                CompilationError = $"Compilation failed: {errorContent}";
            }
        }
        catch (Exception ex)
        {
            CompilationError = $"Error during compilation: {ex.Message}";
        }

        return Page();
    }
}

public class CompilationResult
{
    public bool Success { get; set; }
    public string? GameId { get; set; }
    public string? GameUrl { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Output { get; set; }
}