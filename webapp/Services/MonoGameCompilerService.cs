using Microsoft.Build.Logging;
using Microsoft.Build.Evaluation;
using System.Diagnostics;

namespace webapp.Services;

public class MonoGameCompilerService
{
    private readonly ILogger<MonoGameCompilerService> _logger;
    private readonly string _monoGameProjectPath;
    private readonly string _compiledGamesPath;
    private readonly string _tempBuildPath;

    public MonoGameCompilerService(ILogger<MonoGameCompilerService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _monoGameProjectPath = configuration.GetValue<string>("MonoGameProjectPath") ?? 
            Path.Combine(Directory.GetCurrentDirectory(), "..", "MonoGameVB.Wasm");
        _compiledGamesPath = Path.Combine(Directory.GetCurrentDirectory(), "CompiledGames");
        _tempBuildPath = Path.Combine(Path.GetTempPath(), "MonoGameBuilds");
        
        // Ensure directories exist
        Directory.CreateDirectory(_compiledGamesPath);
        Directory.CreateDirectory(_tempBuildPath);
    }

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
            if (assets != null && assets.Any())
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

    private async Task UpdateGameMainAsync(string projectPath, string vbCode)
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