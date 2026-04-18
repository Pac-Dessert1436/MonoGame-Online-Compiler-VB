using Microsoft.AspNetCore.Mvc;
using webapp.Services;
using webapp.Models;

namespace webapp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectController(UserService userService, ILogger<ProjectController> logger) : ControllerBase
{
    [HttpGet("list")]
    public async Task<ActionResult<List<GameProject>>> GetUserProjects([FromQuery] int userId)
    {
        try
        {
            var projects = await userService.GetUserGameProjectsAsync(userId);
            return Ok(projects);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting projects for user {UserId}", userId);
            return StatusCode(500, "Failed to retrieve projects");
        }
    }

    [HttpGet("{projectId}")]
    public async Task<ActionResult<GameProject>> GetProject(int projectId, [FromQuery] int userId)
    {
        try
        {
            var project = await userService.GetGameProjectAsync(projectId, userId);
            if (project == null)
            {
                return NotFound("Project not found");
            }
            return Ok(project);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting project {ProjectId}", projectId);
            return StatusCode(500, "Failed to retrieve project");
        }
    }

    [HttpPost("create")]
    public async Task<ActionResult<GameProject>> CreateProject([FromBody] CreateProjectRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.VbCode))
        {
            return BadRequest("Project name and VB code are required");
        }

        try
        {
            var project = await userService.CreateGameProjectAsync(request.UserId, request.Name, request.VbCode);
            if (project == null)
            {
                return BadRequest("Failed to create project");
            }
            return Ok(project);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating project for user {UserId}", request.UserId);
            return StatusCode(500, "Failed to create project");
        }
    }

    [HttpPut("{projectId}")]
    public async Task<ActionResult<GameProject>> UpdateProject(int projectId, [FromBody] UpdateProjectRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.VbCode))
        {
            return BadRequest("Project name and VB code are required");
        }

        try
        {
            var project = await userService.UpdateGameProjectAsync(projectId, request.UserId, request.Name, request.VbCode);
            if (project == null)
            {
                return NotFound("Project not found");
            }
            return Ok(project);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating project {ProjectId}", projectId);
            return StatusCode(500, "Failed to update project");
        }
    }

    [HttpDelete("{projectId}")]
    public async Task<ActionResult> DeleteProject(int projectId, [FromQuery] int userId)
    {
        try
        {
            var success = await userService.DeleteGameProjectAsync(projectId, userId);
            if (!success)
            {
                return NotFound("Project not found");
            }
            return Ok(new { Message = "Project deleted successfully" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting project {ProjectId}", projectId);
            return StatusCode(500, "Failed to delete project");
        }
    }

    [HttpPost("{projectId}/assets")]
    public async Task<ActionResult<GameAsset>> AddAsset(int projectId, IFormFile file, [FromQuery] int userId)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File is required");
        }

        try
        {
            var userAssetPath = Path.Combine(Directory.GetCurrentDirectory(), "UserAssets", userId.ToString(), projectId.ToString());
            Directory.CreateDirectory(userAssetPath);

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(userAssetPath, fileName);

            using (var stream = System.IO.File.Create(filePath))
            {
                await file.CopyToAsync(stream);
            }

            var asset = await userService.AddAssetAsync(projectId, userId, fileName, filePath, file.Length, file.ContentType);
            if (asset == null)
            {
                return BadRequest("Failed to add asset");
            }

            return Ok(asset);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding asset to project {ProjectId}", projectId);
            return StatusCode(500, "Failed to add asset");
        }
    }

    [HttpDelete("assets/{assetId}")]
    public async Task<ActionResult> DeleteAsset(int assetId, [FromQuery] int userId)
    {
        try
        {
            var success = await userService.DeleteAssetAsync(assetId, userId);
            if (!success)
            {
                return NotFound("Asset not found");
            }
            return Ok(new { Message = "Asset deleted successfully" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting asset {AssetId}", assetId);
            return StatusCode(500, "Failed to delete asset");
        }
    }

    [HttpGet("{projectId}/history")]
    public async Task<ActionResult<List<CompilationSession>>> GetCompilationHistory(int projectId, [FromQuery] int userId, [FromQuery] int limit = 10)
    {
        try
        {
            var history = await userService.GetCompilationHistoryAsync(projectId, userId, limit);
            return Ok(history);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting compilation history for project {ProjectId}", projectId);
            return StatusCode(500, "Failed to retrieve compilation history");
        }
    }
}

public sealed class CreateProjectRequest
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string VbCode { get; set; } = string.Empty;
}

public sealed class UpdateProjectRequest
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string VbCode { get; set; } = string.Empty;
}