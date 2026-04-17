using Microsoft.EntityFrameworkCore;
using webapp.Models;

namespace webapp.Services;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<GameProject> GameProjects { get; set; }
    public DbSet<GameAsset> GameAssets { get; set; }
    public DbSet<CompilationSession> CompilationSessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<GameProject>()
            .HasOne(p => p.User)
            .WithMany(u => u.GameProjects)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GameAsset>()
            .HasOne(a => a.GameProject)
            .WithMany(p => p.Assets)
            .HasForeignKey(a => a.GameProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CompilationSession>()
            .HasOne(s => s.GameProject)
            .WithMany(p => p.CompilationSessions)
            .HasForeignKey(s => s.GameProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}