using webapp.Services;
using webapp.BackgroundServices;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configure database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=monogame_compiler.db"));

// Configure HTTP client with base address and extended timeout
builder.Services.AddHttpClient("LocalClient", client =>
{
    client.BaseAddress = new Uri("http://localhost:5033");
    client.Timeout = TimeSpan.FromMinutes(5); // 5 minutes timeout for compilation
});

// Register services
builder.Services.AddScoped<MonoGameCompilerService>();
builder.Services.AddScoped<UserService>();

// Register background services
builder.Services.AddHostedService<CacheCleanupService>();

// Configure CORS for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseSession();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthorization();

// Serve static files including compiled games
app.UseStaticFiles();

// Serve compiled game files from a separate directory
var compiledGamesPath = Path.Combine(Directory.GetCurrentDirectory(), "CompiledGames");
if (!Directory.Exists(compiledGamesPath))
{
    Directory.CreateDirectory(compiledGamesPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(compiledGamesPath),
    RequestPath = "/games"
});

app.UseStaticFiles();
app.MapRazorPages();

app.MapControllers();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();
}

app.Run();