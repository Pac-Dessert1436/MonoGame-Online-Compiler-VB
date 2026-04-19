# MonoGame VB.NET Online Compiler

A web-based integrated development environment (IDE) for compiling VB.NET MonoGame games to WebAssembly, enabling high-performance 2D game development that runs natively in modern browsers. The backend portion is implemented using the latest version of C# and ASP.NET Core (see [Technology Stack](#️-technology-stack)).

## 🚧 Status: Work in Progress

This project is currently under active development and is **not yet available on GitHub Pages**. Please stay tuned for updates!

## ✨ Features

- **Full VB.NET Support**: Write games in Visual Basic .NET with syntax highlighting
- **MonoGame Framework 3.8**: Professional-grade game development framework
- **WebAssembly Compilation**: Compile your games to WASM for high-performance browser execution
- **Built-in Code Editor**: Write and edit `GameMain.vb` directly in the browser
- **Event Scheduling Support**: Schedule game events defined in VB.NET modules, using the `ModuleEventRaiser.Generator` package (v1.1.7.9) embedded in the game code
- **Asset Pipeline**: Upload and manage game assets (images, sounds, etc.) with MGCB integration
- **Real-time Compilation**: See compilation errors instantly in the browser
- **Interactive Tutorial**: Comprehensive guide for transitioning from vbPixelGameEngine to MonoGame
- **Session Management**: Each compilation session is tracked and isolated

## 🎯 How It Works

1. **Write Code**: Use the built-in code editor to write your VB.NET game code
2. **Add Assets**: Upload images, sounds, and other game resources (optional)
3. **Compile**: Backend compiles your code to WebAssembly (~30-40 seconds)
4. **Play**: Run your game directly in the browser with no additional setup

## 🏗️ Project Structure

```
MonoGame Online Compiler VB/
├── webapp/                          # ASP.NET Core web application
│   ├── Controllers/
│   │   └── MonoGameController.cs    # API endpoints for compilation
│   ├── Pages/
│   │   ├── Index.cshtml             # Landing page
│   │   ├── VbCodeEditor.cshtml      # Code editor interface
│   │   ├── Tutorial.cshtml          # MonoGame tutorial
│   │   └── GameRunner.cshtml        # Game execution page
│   ├── Services/
│   │   └── MonoGameCompilerService.cs  # Compilation service
│   ├── CompiledGames/               # Output directory for compiled games
│   └── webapp.csproj                # Web application project file
├── MonoGameVB.Wasm/                 # MonoGame project template
│   ├── Content/                     # Game assets
│   ├── GameMain.vb                  # User-editable game code
│   ├── Program.vb                   # WebAssembly initialization (managed)
│   └── MonoGameVB.Wasm.vbproj       # MonoGame project file
└── README.md                        # This file, explaining the project and its features
```

## 🛠️ Technology Stack

### Web Application (C#)
- **ASP.NET Core 10.0**: Modern web framework using C#
- **Razor Pages**: Server-side page framework
- **Microsoft.Build**: MSBuild integration for compilation
- **Bootstrap 5**: Responsive UI framework

### Game Development (VB.NET)
- **MonoGame Framework 3.8**: Cross-platform game framework
- **.NET 8.0**: Runtime for game compilation
- **WebAssembly**: Browser runtime for game execution
- **MGCB**: MonoGame Content Builder for asset processing

## 🚀 Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) for C# **ASP.NET Core 10.0**
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) for VB.NET MonoGame project
- MonoGame Framework 3.8
- WebAssembly workload for .NET

### Installation (For Development)

1. Clone the repository and navigate to the project directory:
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

## 📖 Usage

### Creating Your First Game

1. Navigate to the **Code Editor** page
2. Write your VB.NET game code in the editor
3. (Optional) Upload game assets using the asset upload form
4. Click **Compile** to build your game
5. Wait for compilation to complete (~30-40 seconds)
6. Your game will automatically open in a new tab

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

### Compile Game
```
POST /api/monogame/compile
Content-Type: application/json

{
  "vbCode": "your VB.NET code here",
  "sessionId": "optional-session-id"
}
```

### Compile with Assets
```
POST /api/monogame/compile-with-assets
Content-Type: multipart/form-data

vbCode: [file]
sessionId: [string]
assets: [files...]
```

### Get Game Status
```
GET /api/monogame/status/{gameId}
```

## ⚠️ Known Limitations

- **Syntax Checking**: Syntax errors are not checked in the editor due to lack of Roslyn analyzer integration
- **Compilation Time**: Initial compilation takes 30-40 seconds
- **Asset Size**: Large assets may increase compilation time significantly
- **Browser Compatibility**: Requires modern browsers with WebAssembly support

## 🤝 Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.

### Areas for Contribution

- Add syntax error checking with Roslyn analyzer
- Improve compilation performance
- Add more game templates and examples
- Enhance the tutorial with additional content
- Add support for 3D game development
- Implement asset preview functionality

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