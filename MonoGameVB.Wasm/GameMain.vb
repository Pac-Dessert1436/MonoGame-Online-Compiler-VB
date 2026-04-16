Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input

Public Enum GameState As Short
    TitleScreen = 0
    Playing = 1
    Paused = 2
    GameOver = 3
End Enum

Partial Public Module Essentials
#Region "Events"
    Public Event GameStateChanged(newState As GameState)
    ' TODO: Add more events as needed
#End Region

    Public Function ParamsSum(ParamArray values As Single()) As Single
        Return If(values.Length = 0, 0F, values.Sum())
    End Function

    Public Function ParamsProduct(ParamArray values As Single()) As Single
        If values.Length = 0 OrElse values.Contains(0F) Then Return 0F
        If values.Length = 1 Then Return values(0)
        Return values.Aggregate(1.0F, Function(x, y) x * y)
    End Function

    ' TODO: Add more methods or properties as needed
End Module

Public Class GameMain
    Inherits Game

    Private ReadOnly _graphics As GraphicsDeviceManager
    Private _spriteBatch As SpriteBatch
    Private _gameState As GameState = GameState.TitleScreen

    Public Sub New()
        _graphics = New GraphicsDeviceManager(Me)
        Content.RootDirectory = "Content"
        IsMouseVisible = True
    End Sub

    Private Sub OnGameStateChanged(newState As GameState)
        _gameState = newState
    End Sub

    Protected Overrides Sub Initialize()
        ' TODO: Add your initialization logic here
        AddHandler Essentials.GameStateChanged, AddressOf OnGameStateChanged

        MyBase.Initialize()
    End Sub

    Protected Overrides Sub LoadContent()
        _spriteBatch = New SpriteBatch(GraphicsDevice)

        ' TODO: use Me.Content to load your game content here
    End Sub

    Protected Overrides Sub Update(gameTime As GameTime)
        If GamePad.GetState(PlayerIndex.One).Buttons.Back = ButtonState.Pressed OrElse
            Keyboard.GetState().IsKeyDown(Keys.Escape) Then [Exit]()

        ' TODO: Add your update logic, such as game state management, input handling, etc.

        MyBase.Update(gameTime)
    End Sub

    Protected Overrides Sub Draw(gameTime As GameTime)
        GraphicsDevice.Clear(Color.CornflowerBlue)
        RaiseScheduledEvents()

        With _spriteBatch
            .Begin(samplerState:=SamplerState.PointClamp)

            ' TODO: Add your drawing code for game elements like sprites, backgrounds, etc.

            .End()
        End With

        MyBase.Draw(gameTime)
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            RemoveHandler Essentials.GameStateChanged, AddressOf OnGameStateChanged

            ' TODO: Add more disposal logic (if any) for managed resources here
        End If

        ' TODO: Dispose any unmanaged resources here

        MyBase.Dispose(disposing)
    End Sub
End Class