using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using webapp.Controllers;
using webapp.Models;

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

    [BindProperty]
    public string? CompilationError { get; set; }
    public string? GameUrl { get; set; }
    public bool IsCompiling { get; set; }
    public string? CompilationGameId { get; set; }
    public string? CompilationStatus { get; set; }

    [BindProperty]
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

    public async Task<IActionResult> OnGetAsync(int? projectId, string? compilationGameId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToPage("/UserLogin");
        }

        UserId = userId.Value;
        Username = HttpContext.Session.GetString("Username") ?? "User";
        IsAuthenticated = true;

        // Check compilation status if compilationGameId is provided
        if (!string.IsNullOrEmpty(compilationGameId))
        {
            await CheckCompilationStatusAsync(compilationGameId);
        }

        if (projectId.HasValue)
        {
            ProjectId = projectId.Value;
            await LoadProjectAsync();
        }
        else
        {
            // Create a new project in the database
            try
            {
                var httpClient = _httpClientFactory.CreateClient("LocalClient");
                var createRequest = new { UserId, Name = "New Project", VbCode = ScaffoldCode };
                var response = await httpClient.PostAsJsonAsync("api/project/create", createRequest);
                
                if (response.IsSuccessStatusCode)
                {
                    var project = await response.Content.ReadFromJsonAsync<GameProject>();
                    if (project != null)
                    {
                        ProjectId = project.Id;
                        VbCode = project.VbCode;
                        ProjectName = project.Name;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating new project");
                // Fallback to scaffold code if project creation fails
                VbCode = ScaffoldCode;
                ProjectName = "New Project";
            }
        }

        _logger.LogInformation("OnGetAsync called. ProjectId: {ProjectId}, VbCode length: {Length}", ProjectId, VbCode?.Length ?? 0);
        return Page();
    }

    private async Task CheckCompilationStatusAsync(string compilationGameId)
    {
        try
        {
            var parts = compilationGameId.Split('_');
            if (parts.Length >= 4)
            {
                var sessionId = string.Join('_', parts.Skip(3));
                var httpClient = _httpClientFactory.CreateClient("LocalClient");
                var response = await httpClient.GetAsync($"api/project/session/{sessionId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var session = await response.Content.ReadFromJsonAsync<CompilationSession>();
                    if (session != null && session.CompletedAt.HasValue)
                    {
                        IsCompiling = false;
                        CompilationGameId = null;
                        
                        if (!session.Success)
                        {
                            CompilationError = session.ErrorMessage ?? "Compilation failed";
                            if (!string.IsNullOrEmpty(session.Output))
                            {
                                CompilationError += "\n\nCompilation Output:\n" + session.Output;
                            }
                        }
                        else
                        {
                            GameUrl = $"/GameRunner?gameId={compilationGameId}";
                        }
                    }
                    else
                    {
                        IsCompiling = true;
                        CompilationGameId = compilationGameId;
                        CompilationStatus = "Compiling";
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // Session not found, check if game files exist
                    var gamePath = Path.Combine(Directory.GetCurrentDirectory(), "CompiledGames", compilationGameId);
                    if (Directory.Exists(gamePath))
                    {
                        var indexFile = Path.Combine(gamePath, "index.html");
                        if (System.IO.File.Exists(indexFile))
                        {
                            // Game is ready, redirect to GameRunner
                            IsCompiling = false;
                            CompilationGameId = null;
                        }
                        else
                        {
                            // Still compiling
                            IsCompiling = true;
                            CompilationGameId = compilationGameId;
                            CompilationStatus = "Compiling";
                        }
                    }
                    else
                    {
                        // Game not found
                        IsCompiling = false;
                        CompilationGameId = null;
                        CompilationError = "Game not found. It may have expired or been removed.";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking compilation status for game {GameId}", compilationGameId);
            IsCompiling = false;
            CompilationGameId = null;
            CompilationError = $"Error checking compilation status: {ex.Message}";
        }
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

    public async Task<IActionResult> OnPostCompileAsync()
    {
        _logger.LogInformation("OnPostCompileAsync called");
        _logger.LogInformation("VbCode length: {Length}", VbCode?.Length ?? 0);

        if (!string.IsNullOrEmpty(VbCode))
        {
            var preview = VbCode.Length > 100 ? VbCode[..100] : VbCode;
            _logger.LogInformation("VbCode content preview: {Preview}", preview);
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

            await UpdateProjectAsync();

            var sessionId = Guid.NewGuid().ToString();
            var gameId = $"game_{UserId}_{ProjectId}_{sessionId}";

            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Sending compilation request for game {GameId}", gameId);
                    var response = await httpClient.PostAsJsonAsync("api/monogame/compile-enhanced", new CompileRequest { ProjectId = ProjectId, UserId = UserId, SessionId = sessionId });
                    
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Compilation response for game {GameId}: Status={Status}, Content={Content}", 
                        gameId, response.StatusCode, responseContent);
                        
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError("Compilation request failed for game {GameId}: {StatusCode} - {Content}", 
                            gameId, response.StatusCode, responseContent);
                    }
                }
                catch (TaskCanceledException ex)
                {
                    _logger.LogError(ex, "Compilation request timed out for game {GameId}", gameId);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "HTTP error during compilation for game {GameId}", gameId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error starting compilation for game {GameId}", gameId);
                }
            });

            IsCompiling = true;
            CompilationGameId = gameId;
            CompilationStatus = "Initializing compilation...";
            _logger.LogInformation("Compilation started, showing compiling message for game {GameId}", gameId);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during compilation");
            CompilationError = $"Error during compilation: {ex.Message}";
            return Page();
        }
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
        _logger.LogInformation("VbCode length: {Length}", VbCode?.Length ?? 0);

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

            // Generate a unique session ID for this compilation
            var sessionId = Guid.NewGuid().ToString();
            var gameId = $"game_{UserId}_{ProjectId}_{sessionId}";

            // Start compilation in background
            _ = Task.Run(async () =>
            {
                try
                {
                    using var content = new MultipartFormDataContent();
                    content.Add(new StringContent(ProjectId.ToString()), "ProjectId");
                    content.Add(new StringContent(UserId.ToString()), "UserId");
                    content.Add(new StringContent(sessionId), "SessionId");
                    content.Add(new StringContent(VbCode), "VbCode");

                    if (AssetFiles != null)
                    {
                        foreach (var file in AssetFiles)
                        {
                            content.Add(new StreamContent(file.OpenReadStream()), file.FileName, file.FileName);
                        }
                    }

                    var response = await httpClient.PostAsync("api/monogame/compile-enhanced-with-assets", content);
                    _logger.LogInformation("Compilation with assets started for game {GameId}, status: {Status}", gameId, response.StatusCode);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error starting compilation with assets for game {GameId}", gameId);
                }
            });

            // Set compilation status and return to page
            IsCompiling = true;
            CompilationGameId = gameId;
            CompilationStatus = "Compiling with assets";
            _logger.LogInformation("Compilation with assets started, showing compiling message for game {GameId}", gameId);
            return Page();
        }
        catch (Exception ex)
        {
            CompilationError = $"Error during compilation: {ex.Message}";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostAbortAsync()
    {
        _logger.LogInformation("OnPostAbortAsync called for game {GameId}", CompilationGameId);

        if (string.IsNullOrEmpty(CompilationGameId))
        {
            return Page();
        }

        try
        {
            var parts = CompilationGameId.Split('_');
            if (parts.Length >= 4)
            {
                var sessionId = string.Join('_', parts.Skip(3));
                var httpClient = _httpClientFactory.CreateClient("LocalClient");
                await httpClient.PostAsync($"api/project/abort/{sessionId}", null);
                _logger.LogInformation("Abort request sent for session {SessionId}", sessionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error aborting compilation");
        }

        IsCompiling = false;
        CompilationGameId = null;
        CompilationError = "Compilation aborted by user";
        
        return Page();
    }
}

public sealed class CompilationResult
{
    public bool Success { get; set; }
    public string? GameId { get; set; }
    public string? GameUrl { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Output { get; set; }
}