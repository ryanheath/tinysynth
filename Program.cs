using Raylib_CSharp.Colors;
using Raylib_CSharp.Rendering;
using Raylib_CSharp.Windowing;
using System.Numerics;
using static Raylib_CSharp.Time;

const int screenWidth = 800;
const int screenHeight = 450;

Raylib_CSharp.Raylib.SetConfigFlags(ConfigFlags.HighDpiWindow);
Window.Init(screenWidth, screenHeight, "TinySynth");

Vector2 dpiScale = Window.GetScaleDPI();
int windowWidth = Math.Max(screenWidth, (int)MathF.Round(screenWidth * dpiScale.X));
int windowHeight = Math.Max(screenHeight, (int)MathF.Round(screenHeight * dpiScale.Y));
Window.SetSize(windowWidth, windowHeight);

SetTargetFPS(60);

while (!Window.ShouldClose())
{
    string message = "raylib-csharp is working";
    int fontSize = 20;
    int x = 190;
    int y = 200;

    Graphics.BeginDrawing();
    Graphics.ClearBackground(new Color(245, 245, 245, 255));
    Graphics.DrawText(message, x, y, fontSize, new Color(80, 80, 80, 255));
    Graphics.EndDrawing();
}

Window.Close();
