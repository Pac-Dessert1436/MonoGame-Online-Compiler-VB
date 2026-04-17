using Microsoft.EntityFrameworkCore;
using webapp.Models;

namespace webapp.Services;

public class UserService(AppDbContext context, ILogger<UserService> logger)
{
    public async Task<User?> CreateUserAsync(string username, string email, string password)
    {
        var existingUser = await context.Users
            .FirstOrDefaultAsync(u => u.Email == email || u.Username == username);
        
        if (existingUser != null)
        {
            return null;
        }

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        logger.LogInformation("Created new user: {Username}", username);
        return user;
    }

    public async Task<User?> ValidateUserAsync(string email, string password)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Email == email);
        
        if (user == null)
        {
            return null;
        }

        bool isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        
        if (isValid)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }

        return isValid ? user : null;
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await context.Users
            .Include(u => u.GameProjects)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<GameProject?> CreateGameProjectAsync(int userId, string name, string vbCode)
    {
        var project = new GameProject
        {
            Name = name,
            VbCode = vbCode,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        context.GameProjects.Add(project);
        await context.SaveChangesAsync();

        logger.LogInformation("Created game project: {Name} for user: {UserId}", name, userId);
        return project;
    }

    public async Task<GameProject?> GetGameProjectAsync(int projectId, int userId)
    {
        return await context.GameProjects
            .Include(p => p.Assets)
            .Include(p => p.CompilationSessions.OrderByDescending(s => s.StartedAt).Take(10))
            .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);
    }

    public async Task<List<GameProject>> GetUserGameProjectsAsync(int userId)
    {
        return await context.GameProjects
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
            .ToListAsync();
    }

    public async Task<GameProject?> UpdateGameProjectAsync(int projectId, int userId, string name, string vbCode)
    {
        var project = await context.GameProjects
            .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);
        
        if (project == null)
        {
            return null;
        }

        project.Name = name;
        project.VbCode = vbCode;
        project.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return project;
    }

    public async Task<bool> DeleteGameProjectAsync(int projectId, int userId)
    {
        var project = await context.GameProjects
            .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);
        
        if (project == null)
        {
            return false;
        }

        context.GameProjects.Remove(project);
        await context.SaveChangesAsync();
        
        logger.LogInformation("Deleted game project: {ProjectId}", projectId);
        return true;
    }

    public async Task<GameAsset?> AddAssetAsync(int projectId, int userId, string fileName, string filePath, long fileSize, string contentType)
    {
        var project = await context.GameProjects
            .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);
        
        if (project == null)
        {
            return null;
        }

        var asset = new GameAsset
        {
            FileName = fileName,
            FilePath = filePath,
            FileSize = fileSize,
            ContentType = contentType,
            GameProjectId = projectId,
            UploadedAt = DateTime.UtcNow
        };

        context.GameAssets.Add(asset);
        await context.SaveChangesAsync();

        return asset;
    }

    public async Task<bool> DeleteAssetAsync(int assetId, int userId)
    {
        var asset = await context.GameAssets
            .Include(a => a.GameProject)
            .FirstOrDefaultAsync(a => a.Id == assetId && a.GameProject.UserId == userId);
        
        if (asset == null)
        {
            return false;
        }

        try
        {
            if (File.Exists(asset.FilePath))
            {
                File.Delete(asset.FilePath);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete asset file: {FilePath}", asset.FilePath);
        }

        context.GameAssets.Remove(asset);
        await context.SaveChangesAsync();

        return true;
    }

    public async Task<CompilationSession?> CreateCompilationSessionAsync(int projectId, int userId, string sessionId)
    {
        var project = await context.GameProjects
            .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);
        
        if (project == null)
        {
            return null;
        }

        var session = new CompilationSession
        {
            SessionId = sessionId,
            GameProjectId = projectId,
            StartedAt = DateTime.UtcNow
        };

        context.CompilationSessions.Add(session);
        await context.SaveChangesAsync();

        return session;
    }

    public async Task<CompilationSession?> UpdateCompilationSessionAsync(int sessionId, bool success, string? errorMessage = null, string? output = null, string? compiledGamePath = null)
    {
        var session = await context.CompilationSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId);
        
        if (session == null)
        {
            return null;
        }

        session.Success = success;
        session.ErrorMessage = errorMessage;
        session.Output = output;
        session.CompiledGamePath = compiledGamePath;
        session.CompletedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return session;
    }

    public async Task<List<CompilationSession>> GetCompilationHistoryAsync(int projectId, int userId, int limit = 10)
    {
        return await context.CompilationSessions
            .Where(s => s.GameProjectId == projectId && s.GameProject.UserId == userId)
            .OrderByDescending(s => s.StartedAt)
            .Take(limit)
            .ToListAsync();
    }
}