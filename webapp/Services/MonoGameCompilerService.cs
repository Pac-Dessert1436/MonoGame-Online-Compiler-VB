using System.Diagnostics;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using webapp.Models;

namespace webapp.Services;

public sealed class MonoGameCompilerService
{
    private readonly ILogger<MonoGameCompilerService> _logger;
    private readonly string _monoGameProjectPath;
    private readonly string _compiledGamesPath;
    private readonly string _tempBuildPath;
    private readonly string _userAssetsPath;
    private readonly string _buildCachePath;
    private readonly UserService _userService;
    private readonly SemaphoreSlim _compilationSemaphore;
    private readonly ConcurrentDictionary<string, string> _contentHashCache;

    public MonoGameCompilerService(ILogger<MonoGameCompilerService> logger, IConfiguration configuration, UserService userService)
    {
        _logger = logger;
        _userService = userService;
        _monoGameProjectPath = configuration.GetValue<string>("MonoGameProjectPath") ?? 
            Path.Combine(Directory.GetCurrentDirectory(), "..", "MonoGameVB.Wasm");
        _compiledGamesPath = Path.Combine(Directory.GetCurrentDirectory(), "CompiledGames");
        _tempBuildPath = Path.Combine(Path.GetTempPath(), "MonoGameBuilds");
        _userAssetsPath = Path.Combine(Directory.GetCurrentDirectory(), "UserAssets");
        _buildCachePath = Path.Combine(Path.GetTempPath(), "MonoGameBuildCache");
        
        _compilationSemaphore = new SemaphoreSlim(3);
        _contentHashCache = new ConcurrentDictionary<string, string>();
        
        Directory.CreateDirectory(_compiledGamesPath);
        Directory.CreateDirectory(_tempBuildPath);
        Directory.CreateDirectory(_userAssetsPath);
        Directory.CreateDirectory(_buildCachePath);
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
        _logger.LogInformation("Starting compilation for ProjectId={ProjectId}, UserId={UserId}, SessionId={SessionId}", 
            projectId, userId, sessionId);

        var dbSession = await _userService.CreateCompilationSessionAsync(projectId, userId, sessionId);
        if (dbSession == null)
        {
            _logger.LogError("Failed to create compilation session for ProjectId={ProjectId}, UserId={UserId}", projectId, userId);
            return new CompilationResult
            {
                Success = false,
                ErrorMessage = "Failed to create compilation session"
            };
        }

        _logger.LogInformation("Compilation session created with ID={SessionId}", dbSession.Id);

        await _compilationSemaphore.WaitAsync();
        try
        {
            var project = await _userService.GetGameProjectAsync(projectId, userId);
            if (project == null)
            {
                _logger.LogError("Game project not found for ProjectId={ProjectId}, UserId={UserId}", projectId, userId);
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

            _logger.LogInformation("Compilation paths set - GameId={GameId}, OutputPath={OutputPath}, TempPath={TempPath}", 
                gameId, gameOutputPath, tempProjectPath);

            Directory.CreateDirectory(gameOutputPath);
            Directory.CreateDirectory(tempProjectPath);
            Directory.CreateDirectory(userAssetPath);

            var codeHash = ComputeHash(project.VbCode);
            var cacheKey = $"{userId}_{projectId}";
            var cachedBuildPath = Path.Combine(_buildCachePath, cacheKey);
            var cacheInfoPath = Path.Combine(cachedBuildPath, ".cacheinfo");

            bool useCachedBuild = false;
            
            if (Directory.Exists(cachedBuildPath) && File.Exists(cacheInfoPath))
            {
                var cachedHash = await File.ReadAllTextAsync(cacheInfoPath);
                if (cachedHash == codeHash)
                {
                    useCachedBuild = true;
                    _logger.LogInformation("Using cached build for user {UserId}, project {ProjectId}", userId, projectId);
                }
            }

            await CopyMonoGameProjectOptimizedAsync(tempProjectPath);
            await UpdateGameMainAsync(tempProjectPath, project.VbCode);
            
            if (useCachedBuild)
            {
                await RestoreBuildCacheAsync(cachedBuildPath, tempProjectPath);
            }
            
            await HandleAssetsAsync(tempProjectPath, userAssetPath, project.Assets, newAssets);

            var compilationResult = await CompileToWebAssemblyOptimizedAsync(tempProjectPath, gameOutputPath, useCachedBuild);

            if (compilationResult.Success)
            {
                if (!useCachedBuild)
                {
                    await UpdateBuildCacheAsync(cachedBuildPath, tempProjectPath, codeHash);
                }
                
                await _userService.UpdateCompilationSessionAsync(dbSession.SessionId, true, null, compilationResult.Output, gameOutputPath);
                
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
                await _userService.UpdateCompilationSessionAsync(dbSession.SessionId, false, compilationResult.ErrorMessage, compilationResult.Output);
                
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
            await _userService.UpdateCompilationSessionAsync(dbSession.SessionId, false, $"Compilation error: {ex.Message}");
            
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

    private async Task CopyMonoGameProjectOptimizedAsync(string targetPath)
    {
        if (!Directory.Exists(_monoGameProjectPath))
        {
            throw new DirectoryNotFoundException($"MonoGame project not found at {_monoGameProjectPath}");
        }

        var filesToCopy = Directory.GetFiles(_monoGameProjectPath, "*", SearchOption.AllDirectories)
            .Where(f => !f.Contains("\\obj\\") && !f.Contains("\\bin\\"))
            .ToList();

        var copyTasks = filesToCopy.Select(async file =>
        {
            var relativePath = Path.GetRelativePath(_monoGameProjectPath, file);
            var targetFile = Path.Combine(targetPath, relativePath);
            
            var targetDir = Path.GetDirectoryName(targetFile);
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir!);
            }

            await Task.Run(() => File.Copy(file, targetFile, true));
        });

        await Task.WhenAll(copyTasks);
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

    private async Task<CompilationResult> CompileToWebAssemblyOptimizedAsync(string projectPath, string outputPath, bool useCachedBuild)
    {
        var projectFile = Path.Combine(projectPath, "MonoGameVB.Wasm.vbproj");
        var buildArgs = useCachedBuild 
            ? $"publish \"{projectFile}\" -c Release -o \"{outputPath}\" --runtime browser-wasm /p:WasmBuildNative=true /p:IncrementalBuild=true"
            : $"publish \"{projectFile}\" -c Release -o \"{outputPath}\" --runtime browser-wasm /p:WasmBuildNative=true";

        _logger.LogInformation("Starting compilation with args: {Args}", buildArgs);

        var processInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = buildArgs,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = projectPath,
            Environment = 
            {
                ["MSBUILDDISABLENODEREUSE"] = "1",
                ["UseSharedCompilation"] = "false"
            }
        };

        using var process = Process.Start(processInfo);
        if (process != null)
        {
            _logger.LogInformation("Compilation process started with PID: {ProcessId}", process.Id);
            
            await process.WaitForExitAsync();
            
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            _logger.LogInformation("Compilation process exited with code: {ExitCode}", process.ExitCode);
            
            if (!string.IsNullOrEmpty(output))
            {
                _logger.LogDebug("Compilation output: {Output}", output);
            }
            
            if (!string.IsNullOrEmpty(error))
            {
                _logger.LogWarning("Compilation errors: {Error}", error);
            }

            if (process.ExitCode == 0)
            {
                _logger.LogInformation("Compilation completed successfully");
                return new CompilationResult
                {
                    Success = true,
                    Output = output
                };
            }
            else
            {
                _logger.LogError("Compilation failed with exit code: {ExitCode}", process.ExitCode);
                return new CompilationResult
                {
                    Success = false,
                    ErrorMessage = error,
                    Output = output
                };
            }
        }

        _logger.LogError("Failed to start compilation process");
        return new CompilationResult
        {
            Success = false,
            ErrorMessage = "Failed to start compilation process"
        };
    }

    private static string ComputeHash(string input)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private async Task RestoreBuildCacheAsync(string cachePath, string targetPath)
    {
        try
        {
            var cachedObjPath = Path.Combine(cachePath, "obj");
            var targetObjPath = Path.Combine(targetPath, "obj", "Release");

            if (Directory.Exists(cachedObjPath))
            {
                if (Directory.Exists(targetObjPath))
                {
                    Directory.Delete(targetObjPath, true);
                }

                Directory.CreateDirectory(targetObjPath);
                DirectoryCopy(cachedObjPath, targetObjPath, true);
                _logger.LogInformation("Restored build cache from {CachePath} to {TargetPath}", cachePath, targetPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to restore build cache");
        }
    }

    private async Task UpdateBuildCacheAsync(string cachePath, string sourcePath, string codeHash)
    {
        try
        {
            if (Directory.Exists(cachePath))
            {
                Directory.Delete(cachePath, true);
            }

            Directory.CreateDirectory(cachePath);

            var objPath = Path.Combine(sourcePath, "obj", "Release");
            if (Directory.Exists(objPath))
            {
                var targetObjPath = Path.Combine(cachePath, "obj");
                DirectoryCopy(objPath, targetObjPath, true);
            }

            await File.WriteAllTextAsync(Path.Combine(cachePath, ".cacheinfo"), codeHash);
            _logger.LogInformation("Updated build cache at {CachePath}", cachePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update build cache");
        }
    }

    private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
    {
        var dir = new DirectoryInfo(sourceDirName);
        var dirs = dir.GetDirectories();

        if (!Directory.Exists(destDirName))
        {
            Directory.CreateDirectory(destDirName);
        }

        var files = dir.GetFiles();
        foreach (var file in files)
        {
            var tempPath = Path.Combine(destDirName, file.Name);
            file.CopyTo(tempPath, true);
        }

        if (copySubDirs)
        {
            foreach (var subdir in dirs)
            {
                var tempPath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
            }
        }
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

    public async Task<bool> CleanupOldBuildCacheAsync(int hoursOld = 24)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddHours(-hoursOld);
            var directories = Directory.GetDirectories(_buildCachePath);
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
                        _logger.LogInformation("Deleted old build cache directory: {Directory}", dir);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete build cache directory: {Directory}", dir);
                    }
                }
            }

            _logger.LogInformation("Build cache cleanup completed. Deleted {Count} old cache entries", deletedCount);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup of old build cache");
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