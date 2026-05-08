using System.Numerics;

namespace TinySynth.App.Input;

internal readonly record struct InputDeviceContext(
    Vector2 MousePosition,
    bool MousePressed,
    bool MouseDown,
    bool MouseReleased);
