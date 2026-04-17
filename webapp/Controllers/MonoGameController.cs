using Microsoft.AspNetCore.Mvc;
using webapp.Services;

namespace webapp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MonoGameController(MonoGameCompilerService compilerService, ILogger<MonoGameController> logger) : ControllerBase
{
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
}

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