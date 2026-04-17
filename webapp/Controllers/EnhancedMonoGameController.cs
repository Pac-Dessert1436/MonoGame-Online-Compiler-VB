using Microsoft.AspNetCore.Mvc;
using webapp.Services;
// using webapp.Models;

namespace webapp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EnhancedMonoGameController(EnhancedMonoGameCompilerService compilerService, UserService userService, ILogger<EnhancedMonoGameController> logger) : ControllerBase
{
    [HttpPost("compile")]
    public async Task<ActionResult<CompilationResult>> CompileGame([FromBody] EnhancedCompileRequest request)
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

    [HttpPost("compile-with-new-assets")]
    public async Task<ActionResult<CompilationResult>> CompileGameWithNewAssets([FromForm] EnhancedCompileWithAssetsRequest request)
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