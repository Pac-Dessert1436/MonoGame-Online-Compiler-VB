using Microsoft.AspNetCore.Mvc;
using webapp.Services;

namespace webapp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MonoGameController(MonoGameCompilerService compilerService) : ControllerBase
{
    // Original simple compilation endpoint (from MonoGameController)
    [HttpPost("compile")]
    public async Task<ActionResult<CompilationResult>> CompileGame([FromBody] CompileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.VbCode))
        {
            return BadRequest("VB.NET code is required");
        }

        var sessionId = request.SessionId ?? Guid.NewGuid().ToString();
        var result = await compilerService.CompileGameAsync(request.VbCode, sessionId);

        if (result.Success)
        {
            return Ok(result);
        }
        else
        {
            return BadRequest(result);
        }
    }

    // Enhanced compilation endpoint with project/user tracking (from EnhancedMonoGameController)
    [HttpPost("compile-enhanced")]
    public async Task<ActionResult<CompilationResult>> CompileGameEnhanced([FromBody] EnhancedCompileRequest request)
    {
        if (request.ProjectId <= 0 || request.UserId <= 0)
        {
            return BadRequest("Valid project ID and user ID are required");
        }

        var sessionId = Guid.NewGuid().ToString();
        var result = await compilerService.CompileGameAsync(request.ProjectId, request.UserId, sessionId);

        if (result.Success)
        {
            return Ok(result);
        }
        else
        {
            return BadRequest(result);
        }
    }

    // File-based compilation endpoints (from both controllers)
    [HttpPost("compile-with-assets")]
    public async Task<ActionResult<CompilationResult>> CompileGameWithAssets([FromForm] CompileWithAssetsRequest request)
    {
        if (request.VbCodeFile == null || request.VbCodeFile.Length == 0)
        {
            return BadRequest("VB.NET code file is required");
        }

        using var reader = new StreamReader(request.VbCodeFile.OpenReadStream());
        var vbCode = await reader.ReadToEndAsync();

        var sessionId = request.SessionId ?? Guid.NewGuid().ToString();
        var assets = Request.Form.Files.Where(f => f != request.VbCodeFile).ToList();

        var result = await compilerService.CompileGameAsync(vbCode, sessionId, assets);

        if (result.Success)
        {
            return Ok(result);
        }
        else
        {
            return BadRequest(result);
        }
    }

    [HttpPost("compile-enhanced-with-assets")]
    public async Task<ActionResult<CompilationResult>> CompileGameEnhancedWithAssets([FromForm] EnhancedCompileWithAssetsRequest request)
    {
        if (request.ProjectId <= 0 || request.UserId <= 0)
        {
            return BadRequest("Valid project ID and user ID are required");
        }

        var sessionId = Guid.NewGuid().ToString();
        var newAssets = Request.Form.Files.ToList();

        var result = await compilerService.CompileGameAsync(request.ProjectId, request.UserId, sessionId, newAssets);

        if (result.Success)
        {
            return Ok(result);
        }
        else
        {
            return BadRequest(result);
        }
    }

    // Status and management endpoints
    [HttpGet("status/{gameId}")]
    public ActionResult GetGameStatus(string gameId)
    {
        var gamePath = Path.Combine(Directory.GetCurrentDirectory(), "CompiledGames", gameId);
        
        if (Directory.Exists(gamePath))
        {
            var indexFile = Path.Combine(gamePath, "index.html");
            if (System.IO.File.Exists(indexFile))
            {
                return Ok(new { GameId = gameId, Status = "Ready", GameUrl = $"/games/{gameId}/index.html" });
            }
            else
            {
                return Ok(new { GameId = gameId, Status = "Compiling" });
            }
        }
        else
        {
            return NotFound(new { GameId = gameId, Status = "NotFound" });
        }
    }

    [HttpGet("storage")]
    public async Task<ActionResult<Dictionary<string, long>>> GetStorageUsage()
    {
        var usage = await compilerService.GetStorageUsageAsync();
        return Ok(usage);
    }

    [HttpPost("cleanup")]
    public async Task<ActionResult> CleanupOldGames([FromQuery] int daysOld = 7)
    {
        var success = await compilerService.CleanupOldCompiledGamesAsync(daysOld);
        if (success)
        {
            return Ok(new { Message = $"Cleanup completed successfully. Removed games older than {daysOld} days." });
        }
        else
        {
            return StatusCode(500, "Cleanup failed");
        }
    }
}

// Compilation result model
public class CompilationResult
{
    public bool Success { get; set; }
    public string? GameId { get; set; }
    public string? GameUrl { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Output { get; set; }
}

// Request models from both controllers
public class CompileRequest
{
    public string VbCode { get; set; } = string.Empty;
    public string? SessionId { get; set; }
}

public class CompileWithAssetsRequest
{
    public IFormFile? VbCodeFile { get; set; }
    public string? SessionId { get; set; }
}

public class EnhancedCompileRequest
{
    public int ProjectId { get; set; }
    public int UserId { get; set; }
}

public class EnhancedCompileWithAssetsRequest
{
    public int ProjectId { get; set; }
    public int UserId { get; set; }
}