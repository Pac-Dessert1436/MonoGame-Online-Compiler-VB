Imports System.Runtime.CompilerServices
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics

Public Module SpriteBatchExtensions
    Private Function CreatePixelTexture(gfx As GraphicsDevice, color As Color) As Texture2D
        Dim tex As New Texture2D(gfx, 1, 1)
        tex.SetData({color})
        Return tex
    End Function

    <Extension>
    Public Sub DrawCircle(spriteBatch As SpriteBatch,
            gfx As GraphicsDevice, center As Vector2, radius As Integer, color As Color,
            Optional thickness As Integer = 1)
        If radius < 1 Then Throw New ArgumentException("Radius must be at least 1.")
        If thickness < 1 Then Throw New ArgumentException("Thickness must be at least 1.")

        Using pixel = CreatePixelTexture(gfx, color)
            Dim innerRadius = Math.Max(0, radius - thickness)
            Dim sqrRadius = radius * radius
            Dim sqrInnerRadius = innerRadius * innerRadius

            For x As Integer = -radius To radius
                For y As Integer = -radius To radius
                    Dim sqrDist = x * x + y * y
                    If sqrDist <= sqrRadius AndAlso sqrDist >= sqrInnerRadius Then
                        spriteBatch.Draw(pixel, center + New Vector2(x, y), color)
                    End If
                Next
            Next
        End Using
    End Sub

    <Extension>
    Public Sub FillCircle(spriteBatch As SpriteBatch,
            gfx As GraphicsDevice, center As Vector2, radius As Integer, color As Color)
        If radius < 1 Then Throw New ArgumentException("Radius must be at least 1.")

        Using pixel = CreatePixelTexture(gfx, color)
            With spriteBatch
                For x As Integer = -radius To radius
                    For y As Integer = -radius To radius
                        If x * x + y * y <= radius * radius Then
                            .Draw(pixel, center + New Vector2(x, y), color)
                        End If
                    Next y
                Next x
            End With
        End Using
    End Sub

    <Extension>
    Public Sub DrawLine(spriteBatch As SpriteBatch,
            gfx As GraphicsDevice, start As Vector2, [end] As Vector2, color As Color,
            Optional thickness As Integer = 1)
        If thickness < 0 Then Throw New ArgumentException("Thickness must be at least 1.")

        Dim length = Vector2.Distance(start, [end])
        Dim angle = CSng(Math.Atan2([end].Y - start.Y, [end].X - start.X))

        Using pixel = CreatePixelTexture(gfx, color)
            spriteBatch.Draw(
                pixel,
                start,
                Nothing,
                color,
                angle,
                Vector2.Zero,
                New Vector2(length, thickness),
                SpriteEffects.None,
                0)
        End Using
    End Sub

    <Extension>
    Public Sub DrawTriangle(spriteBatch As SpriteBatch,
            gfx As GraphicsDevice, point1 As Vector2, point2 As Vector2, point3 As Vector2,
            color As Color, Optional thickness As Integer = 1)
        If thickness < 0 Then Throw New ArgumentException("Thickness must be at least 1.")

        Using pixel = CreatePixelTexture(gfx, color)
            ' Draw lines between the three points
            DrawLine(spriteBatch, gfx, point1, point2, color, thickness)
            DrawLine(spriteBatch, gfx, point2, point3, color, thickness)
            DrawLine(spriteBatch, gfx, point3, point1, color, thickness)
        End Using
    End Sub

    <Extension>
    Public Sub FillTriangle(spriteBatch As SpriteBatch,
            gfx As GraphicsDevice, point1 As Vector2, point2 As Vector2, point3 As Vector2, color As Color)
        Dim IsPointInTriangle = Function(p As Vector2, p0 As Vector2, p1 As Vector2, p2 As Vector2) As Boolean
                                    Dim dX = p.X - p2.X
                                    Dim dY = p.Y - p2.Y
                                    Dim dX21 = p2.X - p1.X
                                    Dim dY12 = p1.Y - p2.Y
                                    Dim D = dY12 * (p0.X - p2.X) + dX21 * (p0.Y - p2.Y)
                                    Dim s = dY12 * dX + dX21 * dY
                                    Dim t = (p2.Y - p0.Y) * dX + (p0.X - p2.X) * dY

                                    If D < 0 Then Return s <= 0 AndAlso t <= 0 AndAlso s + t >= D
                                    Return s >= 0 AndAlso t >= 0 AndAlso s + t <= D
                                End Function

        Using pixel = CreatePixelTexture(gfx, color)
            With spriteBatch
                ' Find bounding box
                Dim minX = CInt(Math.Min(Math.Min(point1.X, point2.X), point3.X))
                Dim maxX = CInt(Math.Max(Math.Max(point1.X, point2.X), point3.X))
                Dim minY = CInt(Math.Min(Math.Min(point1.Y, point2.Y), point3.Y))
                Dim maxY = CInt(Math.Max(Math.Max(point1.Y, point2.Y), point3.Y))

                ' Check each pixel in the bounding box
                For x As Integer = minX To maxX
                    For y As Integer = minY To maxY
                        If IsPointInTriangle(New Vector2(x, y), point1, point2, point3) Then
                            .Draw(pixel, New Vector2(x, y), color)
                        End If
                    Next y
                Next x
            End With
        End Using
    End Sub

    <Extension>
    Public Sub DrawRectangle(spriteBatch As SpriteBatch,
            gfx As GraphicsDevice, rect As Rectangle, thickness As Integer, color As Color)
        Using pixel = CreatePixelTexture(gfx, color)
            With spriteBatch
                .Draw(pixel, New Rectangle(rect.X, rect.Y, rect.Width, thickness), color)
                .Draw(pixel, New Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color)
                .Draw(pixel, New Rectangle(rect.X, rect.Y, thickness, rect.Height), color)
                .Draw(pixel, New Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color)
            End With
        End Using
    End Sub

    <Extension>
    Public Sub FillRectangle(spriteBatch As SpriteBatch,
            gfx As GraphicsDevice, rect As Rectangle, color As Color)
        Using pixel = CreatePixelTexture(gfx, color)
            spriteBatch.Draw(pixel, rect, color)
        End Using
    End Sub

    <Extension>
    Public Sub DrawPolygon(spriteBatch As SpriteBatch,
            gfx As GraphicsDevice, points As Vector2(), color As Color, Optional thickness As Integer = 1)
        If points.Length < 2 Then Throw New ArgumentException("At least 2 points are required.")

        Using pixel = CreatePixelTexture(gfx, color)
            With spriteBatch
                For i As Integer = 0 To points.Length - 2
                    DrawLine(spriteBatch, gfx, points(i), points(i + 1), color, thickness)
                Next i
                ' Close the polygon
                DrawLine(spriteBatch, gfx, points(points.Length - 1), points(0), color, thickness)
            End With
        End Using
    End Sub
End Module