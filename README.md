# MonoGame VB.NET Online Compiler

A web-based integrated development environment (IDE) for compiling VB.NET MonoGame games to WebAssembly, enabling high-performance 2D game development that runs natively in modern browsers. The backend portion is implemented using the latest version of C# and ASP.NET Core (see [Technology Stack](#️-technology-stack)).

## 🚧 Project Status

This is a full-stack web application with a C# ASP.NET Core backend and a web-based frontend. It requires a web server to run and **cannot be deployed directly to GitHub Pages**.

_Please stay tuned for updates and improvements until the project is ready for production._

### Current Status: Active Development ✅

- **Backend**: ASP.NET Core 10.0 with multi-user support
- **Frontend**: Razor Pages with real-time compilation feedback
- **Compilation**: Optimized with build caching (15-20 seconds)
- **Database**: SQLite for user/project/session management
- **Deployment**: Requires web server (IIS, Azure, AWS, etc.)

### Recent Improvements

- **Build Caching**: 50%+ faster subsequent compilations
- **Progress Tracking**: Real-time compilation progress with visual feedback
- **Multi-User Support**: User authentication and project isolation
- **Enhanced Logging**: Detailed compilation logs for debugging
- **Background Services**: Automatic cache cleanup and maintenance
> See [Change Log](CHANGELOG.md) for details.

## ✨ Features

- **Full VB.NET Support**: Write games in Visual Basic .NET with syntax highlighting
- **MonoGame Framework 3.8**: Professional-grade game development framework
- **WebAssembly Compilation**: Compile your games to WASM for high-performance browser execution
- **Built-in Code Editor**: Write and edit `GameMain.vb` directly in browser with Monaco Editor
- **Multi-User System**: User registration, login, and project management
- **Build Caching**: Optimized compilation with artifact caching (50%+ faster rebuilds)
- **Real-time Progress**: Visual compilation progress bar with status updates
- **Event Scheduling Support**: Schedule game events defined in VB.NET modules, using the `ModuleEventRaiser.Generator` package (v1.1.7.9) embedded in the game code
- **Asset Pipeline**: Upload and manage game assets (images, sounds, etc.) with MGCB integration
- **Compilation History**: Track all compilation sessions with success/failure status
- **Interactive Tutorial**: Comprehensive guide for transitioning from vbPixelGameEngine to MonoGame
- **Session Management**: Each compilation session is tracked and isolated
- **Automatic Cleanup**: Background service manages build cache and old compiled games

## 🎯 How It Works

1. **Register/Login**: Create an account or log in to access the IDE
2. **Create Project**: Start a new project or load an existing one
3. **Write Code**: Use the built-in Monaco code editor to write your VB.NET game code
4. **Add Assets**: Upload images, sounds, and other game resources (optional)
5. **Compile**: Backend compiles your code to WebAssembly (~15-20 seconds)
6. **Play**: Run your game directly in the browser with no additional setup

### Compilation Performance

- **First compilation**: ~20 seconds (full build)
- **Subsequent compilations**: ~10-15 seconds (with build cache)
- **With assets**: May take longer depending on asset size
- **Real-time feedback**: Progress bar shows compilation stages

## 🏗️ Project Structure

```
MonoGame Online Compiler VB/
├── webapp/                          # ASP.NET Core web application
│   ├── Controllers/
│   │   └── MonoGameController.cs    # API endpoints for compilation
│   ├── Pages/
│   │   ├── Index.cshtml             # Landing page
│   │   ├── UserLogin.cshtml          # User login page
│   │   ├── UserRegister.cshtml       # User registration page
│   │   ├── VbCodeEditor.cshtml      # Code editor interface
│   │   ├── Tutorial.cshtml          # MonoGame tutorial
│   │   └── GameRunner.cshtml        # Game execution page
│   ├── Services/
│   │   ├── MonoGameCompilerService.cs  # Compilation service with caching
│   │   └── UserService.cs           # User and project management
│   ├── BackgroundServices/
│   │   └── CacheCleanupService.cs    # Automatic cache cleanup
│   ├── Models/
│   │   ├── AppDbContext.cs          # Database context
│   │   ├── GameProject.cs           # Project model
│   │   ├── GameAsset.cs            # Asset model
│   │   ├── CompilationSession.cs     # Session tracking
│   │   └── User.cs                 # User model
│   ├── CompiledGames/               # Output directory for compiled games
│   ├── UserAssets/                 # User-uploaded assets storage
│   └── webapp.csproj                # Web application project file
├── MonoGameVB.Wasm/                 # MonoGame project template
│   ├── Content/                     # Game assets
│   ├── GameMain.vb                  # User-editable game code
│   ├── Program.vb                   # WebAssembly initialization (managed)
│   └── MonoGameVB.Wasm.vbproj       # MonoGame project file
├── MULTI_USER_ARCHITECTURE.md       # Multi-user system documentation
└── README.md                        # This file, explaining the project
```

## 🛠️ Technology Stack

### Web Application (C#)
- **ASP.NET Core 10.0**: Modern web framework using C#
- **Razor Pages**: Server-side page framework
- **Entity Framework Core**: Database ORM for data management
- **SQLite Database**: Lightweight database for users, projects, and sessions
- **Microsoft.Build**: MSBuild integration for compilation
- **Bootstrap 5**: Responsive UI framework
- **Monaco Editor**: Professional code editor with syntax highlighting

### Game Development (VB.NET)
- **MonoGame Framework 3.8**: Cross-platform game framework
- **.NET 8.0**: Runtime for game compilation
- **WebAssembly**: Browser runtime for game execution
- **MGCB**: MonoGame Content Builder for asset processing

### Background Services
- **Hosted Services**: Background task execution
- **Cache Management**: Automatic build cache cleanup
- **Compilation Tracking**: Session monitoring and status updates

## 🚀 Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) for C# ASP.NET Core 10.0
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) for VB.NET MonoGame project
- MonoGame Framework 3.8
- WebAssembly workload for .NET

### Installation (For Development)

1. Clone the repository:
```bash
git clone https://github.com/Pac-Dessert1436/MonoGame-VB-Online-Compiler.git
cd MonoGame-VB-Online-Compiler
```

2. Restore dependencies:
```bash
cd webapp
dotnet restore
```

3. Configure the application:
Edit `appsettings.json` to set the MonoGame project path:
```json
{
  "MonoGameProjectPath": "..\\MonoGameVB.Wasm"
}
```

4. Run the application:
```bash
dotnet run
```

5. Open your browser and navigate to:
```
https://localhost:7287
```

6. Register a new account and start creating games!

### Deployment

This is a **full-stack application** that requires a web server. It cannot be deployed to GitHub Pages.

#### Deployment Options:

- **Azure App Service**: Deploy as a web app with built-in database
- **AWS Elastic Beanstalk**: Deploy to AWS with RDS database
- **DigitalOcean App Platform**: Container-based deployment
- **IIS**: Deploy to Windows Server with IIS
- **Docker**: Containerize the application for any cloud provider

#### Deployment Steps:

1. Build the application:
```bash
cd webapp
dotnet publish -c Release -o ./publish
```

2. Deploy the `publish` folder to your web server
3. Configure environment variables (connection strings, paths)
4. Ensure MonoGame project template is accessible
5. Set up proper file permissions for `CompiledGames` and `UserAssets` directories

## 📖 Usage

### Creating Your First Game

1. **Register/Login**: Create a new account or log in to access the IDE
2. **Create Project**: Click "New Project" to start a new game project
3. **Write Code**: Use the Monaco code editor to write your VB.NET game code
4. **Add Assets**: Upload images, sounds, and other game resources (optional)
5. **Compile**: Click "Run Game" to build your game (~15-20 seconds)
6. **Play**: Your game will automatically open in a new tab when compilation completes

### Project Management

- **Multiple Projects**: Create and manage multiple game projects
- **Save/Load**: Your code and assets are automatically saved
- **Compilation History**: View past compilation attempts and results
- **Asset Management**: Upload, view, and delete game assets
- **Cache Control**: Clear build cache if needed (useful for troubleshooting)

### Code Structure

The compiler manages two key files:

- **Program.vb**: Managed by the backend - handles WebAssembly initialization
- **GameMain.vb**: User-editable - contains your game logic

You only need to edit `GameMain.vb`. The backend ensures proper WebAssembly setup automatically.

### Example Game Code

```vb
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input

Public Class GameMain
    Inherits Game

    Private ReadOnly _graphics As GraphicsDeviceManager
    Private _spriteBatch As SpriteBatch
    Private _position As Vector2 = New Vector2(100.0F, 100.0F)
    Private _speed As Single = 200.0F

    Public Sub New()
        _graphics = New GraphicsDeviceManager(Me)
        Content.RootDirectory = "Content"
        IsMouseVisible = True
    End Sub

    Protected Overrides Sub LoadContent()
        _spriteBatch = New SpriteBatch(GraphicsDevice)
    End Sub

    Protected Overrides Sub Update(gameTime As GameTime)
        Dim keyState As KeyboardState = Keyboard.GetState()
        Dim deltaTime As Single = CSng(gameTime.ElapsedGameTime.TotalSeconds)

        If keyState.IsKeyDown(Keys.W) Then
            _position.Y -= _speed * deltaTime
        End If
        If keyState.IsKeyDown(Keys.S) Then
            _position.Y += _speed * deltaTime
        End If
        If keyState.IsKeyDown(Keys.A) Then
            _position.X -= _speed * deltaTime
        End If
        If keyState.IsKeyDown(Keys.D) Then
            _position.X += _speed * deltaTime
        End If

        MyBase.Update(gameTime)
    End Sub

    Protected Overrides Sub Draw(gameTime As GameTime)
        GraphicsDevice.Clear(Color.CornflowerBlue)

        _spriteBatch.Begin()
        ' Draw your game elements here
        _spriteBatch.End()

        MyBase.Draw(gameTime)
    End Sub
End Class
```

## 📚 Tutorial

The project includes a comprehensive tutorial for developers transitioning from **vbPixelGameEngine** to **MonoGame**. The tutorial covers:

- Overview of MonoGame framework
- Vector types and mathematics
- Drawing methods and sprites
- Game loop architecture
- Code examples and patterns
- Pro tips and best practices

Access the tutorial from the main page or navigate directly to `/Tutorial`.

## 🔧 API Endpoints

### Authentication
```
POST /api/user/register
POST /api/user/login
GET /api/user/logout
```

### Project Management
```
GET /api/project/list?userId={userId}
POST /api/project/create
PUT /api/project/{projectId}?userId={userId}
GET /api/project/{projectId}?userId={userId}
DELETE /api/project/{projectId}?userId={userId}
```

### Compilation
```
POST /api/monogame/compile-enhanced
Content-Type: application/json

{
  "projectId": 1,
  "userId": 1,
  "sessionId": "optional-session-id"
}

Response: {
  "success": true,
  "gameId": "game_1_1_abc123...",
  "gameUrl": "/games/game_1_1_abc123.../index.html",
  "message": "Game compiled successfully!"
}
```

### Compile with New Assets
```
POST /api/monogame/compile-enhanced-with-assets?projectId=1&userId=1
Content-Type: multipart/form-data

files: [binary file data...]

Response: {
  "success": true,
  "gameId": "game_1_1_abc123...",
  "gameUrl": "/games/game_1_1_abc123.../index.html",
  "message": "Game compiled successfully!"
}
```

### Get Game Status
```
GET /api/monogame/status/{gameId}

Response: {
  "gameId": "game_1_1_abc123...",
  "status": "Ready",
  "gameUrl": "/games/game_1_1_abc123.../index.html"
}
```

### Session Management
```
GET /api/project/session/{sessionId}

Response: {
  "id": 1,
  "sessionId": "abc123...",
  "gameProjectId": 1,
  "startedAt": "2024-01-15T10:30:00Z",
  "completedAt": "2024-01-15T10:30:20Z",
  "success": true,
  "errorMessage": null,
  "output": "...",
  "compiledGamePath": "..."
}
```

### Cache Management
```
POST /api/monogame/cleanup-cache?hoursOld=24
DELETE /api/monogame/cache/{userId}/{projectId}
GET /api/monogame/storage

Response: {
  "CompiledGames": 123456789,
  "UserAssets": 98765432,
  "Total": 222222221
}
```

## ⚠️ Known Limitations

- **Syntax Checking**: Syntax errors are not checked in the editor due to lack of Roslyn analyzer integration
- **Compilation Time**: First compilation takes ~20 seconds, subsequent compilations ~10-15 seconds
- **Asset Size**: Large assets may increase compilation time significantly
- **Browser Compatibility**: Requires modern browsers with WebAssembly support
- **Server Requirements**: Requires a web server (cannot be deployed to GitHub Pages)
- **Concurrent Builds**: Limited to 3 concurrent compilations to prevent server overload

### Performance Notes

- **Build Caching**: Subsequent compilations are 50%+ faster with caching
- **Progress Tracking**: Real-time feedback during compilation
- **Automatic Cleanup**: Old cache entries are cleaned every 6 hours
- **Session Timeout**: Maximum 2-minute wait time for compilation completion

## 🐛 Recent Bug Fixes

### Session ID Synchronization (Critical Fix)
- **Issue**: Compilation progress bar stuck at 95% due to session ID mismatch
- **Root Cause**: Backend was generating different session ID than frontend expected
- **Fix**: Synchronized session ID generation between frontend and backend
- **Impact**: Compilation now completes and redirects properly to GameRunner

### Dependency Injection Fix
- **Issue**: Background service failed to inject scoped services
- **Root Cause**: CacheCleanupService (singleton) tried to inject MonoGameCompilerService (scoped)
- **Fix**: Use IServiceScopeFactory to create scoped service instances
- **Impact**: Background cache cleanup now works correctly

### Enhanced Error Handling
- **Issue**: Poor error messages when compilation fails
- **Fix**: Added detailed logging throughout compilation process
- **Impact**: Better debugging and user feedback

### MSBuild Parameter Optimization
- **Issue**: `/p:SkipCompilerExecution=true` parameter caused compilation issues
- **Fix**: Removed problematic parameter, use standard incremental builds
- **Impact**: More reliable compilation process

## 🤝 Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.

### Areas for Contribution

- **Frontend**: Enhance UI/UX, add more editor features
- **Backend**: Improve compilation performance, add caching strategies
- **Database**: Optimize queries, add analytics
- **Features**: 
  - Add syntax error checking with Roslyn analyzer
  - Implement asset preview functionality
  - Add more game templates and examples
  - Enhance the tutorial with additional content
  - Add support for 3D game development
  - Implement real-time collaboration features
- **Testing**: Add unit tests and integration tests
- **Documentation**: Improve API documentation and user guides

### Development Guidelines

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Code Style

- **C#**: Follow Microsoft C# coding conventions
- **VB.NET**: Follow Visual Basic coding conventions
- **JavaScript**: Use modern ES6+ features
- **HTML/CSS**: Follow web accessibility best practices

## 🙏 Acknowledgments

- **MonoGame Team**: For creating an excellent cross-platform game framework
- **Microsoft**: For .NET and ASP.NET Core
- **vbPixelGameEngine Community**: For inspiration and API design patterns

## 🔗 Resources

- [MonoGame Documentation](https://docs.monogame.net/)
- [MonoGame Community Forums](https://community.monogame.net/)
- [MonoGame GitHub Repository](https://github.com/MonoGame/MonoGame)
- [.NET WebAssembly Documentation](https://learn.microsoft.com/en-us/dotnet/core/runtime-config/compilation/wasm)

## 📧 Contact

For questions, suggestions, or feedback, please open an issue on GitHub.

## 📝 License

This project is licensed under the BSD 3-Clause License. See the [LICENSE](LICENSE) file for details.
