using Microsoft.Build.Logging;
using Microsoft.Build.Evaluation;
using System.Diagnostics;
using webapp.Models;

namespace webapp.Services;

public sealed class MonoGameCompilerService
{
    private readonly ILogger<MonoGameCompilerService> _logger;
    private readonly string _monoGameProjectPath;
    private readonly string _compiledGamesPath;
    private readonly string _tempBuildPath;
    private readonly string _userAssetsPath;
    private readonly UserService _userService;
    private readonly SemaphoreSlim _compilationSemaphore;

    public MonoGameCompilerService(ILogger<MonoGameCompilerService> logger, IConfiguration configuration, UserService userService)
    {
        _logger = logger;
        _userService = userService;
        _monoGameProjectPath = configuration.GetValue<string>("MonoGameProjectPath") ?? 
            Path.Combine(Directory.GetCurrentDirectory(), "..", "MonoGameVB.Wasm");
        _compiledGamesPath = Path.Combine(Directory.GetCurrentDirectory(), "CompiledGames");
        _tempBuildPath = Path.Combine(Path.GetTempPath(), "MonoGameBuilds");
        _userAssetsPath = Path.Combine(Directory.GetCurrentDirectory(), "UserAssets");
        
        _compilationSemaphore = new SemaphoreSlim(2); // Limit concurrent compilations to 2
        
        // Ensure directories exist
        Directory.CreateDirectory(_compiledGamesPath);
        Directory.CreateDirectory(_tempBuildPath);
        Directory.CreateDirectory(_userAssetsPath);
    }

    // Method 1: Simple compilation with just VB code (from original MonoGameCompilerService)
    public async Task<CompilationResult> CompileGameAsync(string vbCode, string sessionId, List<IFormFile>? assets = null)
    {
        var gameId = $"game_{sessionId}_{DateTime.UtcNow:yyyyMMddHHmmss}";
        var gameOutputPath = Path.Combine(_compiledGamesPath, gameId);
        var tempProjectPath = Path.Combine(_tempBuildPath, gameId);
        
        try
        {
            Directory.CreateDirectory(gameOutputPath);
            Directory.CreateDirectory(tempProjectPath);

            // Copy MonoGame project to temp location
            await CopyMonoGameProjectAsync(tempProjectPath);

            // Replace GameMain.vb with user code
            await UpdateGameMainAsync(tempProjectPath, vbCode);

            // Handle assets if provided
            if (assets != null && assets.Count != 0)
            {
                await HandleAssetsAsync(tempProjectPath, assets);
            }

            // Compile to WebAssembly
            var compilationResult = await CompileToWebAssemblyAsync(tempProjectPath, gameOutputPath);

            if (compilationResult.Success)
            {
                _logger.LogInformation("Successfully compiled game {GameId}", gameId);
                return new CompilationResult
                {
                    Success = true,
                    GameId = gameId,
                    GameUrl = $"/games/{gameId}/index.html",
                    Message = "Game compiled successfully!"
                };
            }
            else
            {
                _logger.LogError("Failed to compile game {GameId}: {Error}", gameId, compilationResult.ErrorMessage);
                return new CompilationResult
                {
                    Success = false,
                    ErrorMessage = compilationResult.ErrorMessage,
                    Output = compilationResult.Output
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compiling game {GameId}", gameId);
            return new CompilationResult
            {
                Success = false,
                ErrorMessage = $"Compilation error: {ex.Message}"
            };
        }
        finally
        {
            // Cleanup temp directory
            if (Directory.Exists(tempProjectPath))
            {
                try
                {
                    Directory.Delete(tempProjectPath, true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cleanup temp directory {TempPath}", tempProjectPath);
                }
            }
        }
    }

    // Method 2: Enhanced compilation with project/user tracking (from EnhancedMonoGameCompilerService)
    public async Task<CompilationResult> CompileGameAsync(int projectId, int userId, string sessionId, List<IFormFile>? newAssets = null)
    {
        var dbSession = await _userService.CreateCompilationSessionAsync(projectId, userId, sessionId);
        if (dbSession == null)
        {
            return new CompilationResult
            {
                Success = false,
                ErrorMessage = "Failed to create compilation session"
            };
        }

        await _compilationSemaphore.WaitAsync();
        try
        {
            var project = await _userService.GetGameProjectAsync(projectId, userId);
            if (project == null)
            {
                return new CompilationResult
                {
                    Success = false,
                    ErrorMessage = "Game project not found"
                };
            }

            var gameId = $"game_{userId}_{projectId}_{sessionId}";
            var gameOutputPath = Path.Combine(_compiledGamesPath, gameId);
            var tempProjectPath = Path.Combine(_tempBuildPath, gameId);
            var userAssetPath = Path.Combine(_userAssetsPath, userId.ToString(), projectId.ToString());

            Directory.CreateDirectory(gameOutputPath);
            Directory.CreateDirectory(tempProjectPath);
            Directory.CreateDirectory(userAssetPath);

            // Copy MonoGame project to temp location
            await CopyMonoGameProjectAsync(tempProjectPath);

            // Replace GameMain.vb with user code
            await UpdateGameMainAsync(tempProjectPath, project.VbCode);

            // Handle existing and new assets
            await HandleAssetsAsync(tempProjectPath, userAssetPath, project.Assets, newAssets);

            // Compile to WebAssembly
            var compilationResult = await CompileToWebAssemblyAsync(tempProjectPath, gameOutputPath);

            if (compilationResult.Success)
            {
                await _userService.UpdateCompilationSessionAsync(dbSession.Id, true, null, compilationResult.Output, gameOutputPath);
                
                _logger.LogInformation("Successfully compiled game {GameId} for user {UserId}", gameId, userId);
                return new CompilationResult
                {
                    Success = true,
                    GameId = gameId,
                    GameUrl = $"/games/{gameId}/index.html",
                    Message = "Game compiled successfully!"
                };
            }
            else
            {
                await _userService.UpdateCompilationSessionAsync(dbSession.Id, false, compilationResult.ErrorMessage, compilationResult.Output);
                
                _logger.LogError("Failed to compile game {GameId} for user {UserId}: {Error}", gameId, userId, compilationResult.ErrorMessage);
                return new CompilationResult
                {
                    Success = false,
                    ErrorMessage = compilationResult.ErrorMessage,
                    Output = compilationResult.Output
                };
            }
        }
        catch (Exception ex)
        {
            await _userService.UpdateCompilationSessionAsync(dbSession.Id, false, $"Compilation error: {ex.Message}");
            
            _logger.LogError(ex, "Error compiling game for user {UserId}, project {ProjectId}", userId, projectId);
            return new CompilationResult
            {
                Success = false,
                ErrorMessage = $"Compilation error: {ex.Message}"
            };
        }
        finally
        {
            _compilationSemaphore.Release();
            
            // Cleanup temp directory
            var tempProjectPath = Path.Combine(_tempBuildPath, $"game_{userId}_{projectId}_{sessionId}");
            if (Directory.Exists(tempProjectPath))
            {
                try
                {
                    Directory.Delete(tempProjectPath, true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cleanup temp directory {TempPath}", tempProjectPath);
                }
            }
        }
    }

    // Shared private methods
    private async Task CopyMonoGameProjectAsync(string targetPath)
    {
        if (!Directory.Exists(_monoGameProjectPath))
        {
            throw new DirectoryNotFoundException($"MonoGame project not found at {_monoGameProjectPath}");
        }

        // Copy all files except obj and bin directories
        foreach (var file in Directory.GetFiles(_monoGameProjectPath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(_monoGameProjectPath, file);
            var targetFile = Path.Combine(targetPath, relativePath);
            
            var targetDir = Path.GetDirectoryName(targetFile);
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir!);
            }

            await Task.Run(() => File.Copy(file, targetFile, true));
        }
    }

    private static async Task UpdateGameMainAsync(string projectPath, string vbCode)
    {
        var gameMainPath = Path.Combine(projectPath, "GameMain.vb");
        await File.WriteAllTextAsync(gameMainPath, vbCode);
    }

    private async Task HandleAssetsAsync(string projectPath, List<IFormFile> assets)
    {
        var contentPath = Path.Combine(projectPath, "Content");
        
        foreach (var asset in assets)
        {
            var assetPath = Path.Combine(contentPath, asset.FileName);
            using var stream = File.Create(assetPath);
            await asset.CopyToAsync(stream);
        }

        // Run MGCB to build content
        await RunMGCBAsync(projectPath);
    }

    private async Task HandleAssetsAsync(string projectPath, string userAssetPath, ICollection<GameAsset> existingAssets, List<IFormFile>? newAssets)
    {
        var contentPath = Path.Combine(projectPath, "Content");
        
        // Copy existing assets
        foreach (var asset in existingAssets)
        {
            if (File.Exists(asset.FilePath))
            {
                var targetPath = Path.Combine(contentPath, asset.FileName);
                await Task.Run(() => File.Copy(asset.FilePath, targetPath, true));
            }
        }

        // Handle new assets
        if (newAssets != null && newAssets.Count != 0)
        {
            foreach (var asset in newAssets)
            {
                var assetPath = Path.Combine(userAssetPath, asset.FileName);
                using var stream = File.Create(assetPath);
                await asset.CopyToAsync(stream);
                
                // Copy to project content folder
                var targetPath = Path.Combine(contentPath, asset.FileName);
                await Task.Run(() => File.Copy(assetPath, targetPath, true));
            }
        }

        // Run MGCB to build content
        await RunMGCBAsync(projectPath);
    }

    private async Task RunMGCBAsync(string projectPath)
    {
        var mgcbPath = Path.Combine(projectPath, "Content", "Content.mgcb");
        var processInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"mgcb-editor \"{mgcbPath}\" /rebuild",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = projectPath
        };

        using var process = Process.Start(processInfo);
        if (process != null)
        {
            await process.WaitForExitAsync();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            
            if (process.ExitCode != 0)
            {
                _logger.LogWarning("MGCB build completed with warnings: {Error}", error);
            }
        }
    }

    private async Task<CompilationResult> CompileToWebAssemblyAsync(string projectPath, string outputPath)
    {
        var projectFile = Path.Combine(projectPath, "MonoGameVB.Wasm.vbproj");
        var processInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"publish \"{projectFile}\" -c Release -o \"{outputPath}\" --runtime browser-wasm /p:WasmBuildNative=true",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = projectPath
        };

        using var process = Process.Start(processInfo);
        if (process != null)
        {
            await process.WaitForExitAsync();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            if (process.ExitCode == 0)
            {
                return new CompilationResult
                {
                    Success = true,
                    Output = output
                };
            }
            else
            {
                return new CompilationResult
                {
                    Success = false,
                    ErrorMessage = error,
                    Output = output
                };
            }
        }

        return new CompilationResult
        {
            Success = false,
            ErrorMessage = "Failed to start compilation process"
        };
    }

    public async Task<bool> CleanupOldCompiledGamesAsync(int daysOld = 7)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
            var directories = Directory.GetDirectories(_compiledGamesPath);
            var deletedCount = 0;

            foreach (var dir in directories)
            {
                var dirInfo = new DirectoryInfo(dir);
                if (dirInfo.CreationTimeUtc < cutoffDate)
                {
                    try
                    {
                        Directory.Delete(dir, true);
                        deletedCount++;
                        _logger.LogInformation("Deleted old compiled game directory: {Directory}", dir);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete directory: {Directory}", dir);
                    }
                }
            }

            _logger.LogInformation("Cleanup completed. Deleted {Count} old compiled games", deletedCount);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup of old compiled games");
            return false;
        }
    }

    public async Task<Dictionary<string, long>> GetStorageUsageAsync()
    {
        var result = new Dictionary<string, long>();

        try
        {
            var compiledGamesSize = Directory.GetFiles(_compiledGamesPath, "*", SearchOption.AllDirectories)
                .Sum(f => new FileInfo(f).Length);
            
            var userAssetsSize = Directory.GetFiles(_userAssetsPath, "*", SearchOption.AllDirectories)
                .Sum(f => new FileInfo(f).Length);

            result["CompiledGames"] = compiledGamesSize;
            result["UserAssets"] = userAssetsSize;
            result["Total"] = compiledGamesSize + userAssetsSize;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating storage usage");
        }

        return result;
    }
}

// Compilation result model
public sealed class CompilationResult
{
    public bool Success { get; set; }
    public string? GameId { get; set; }
    public string? GameUrl { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Output { get; set; }
}