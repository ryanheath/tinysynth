using System.Numerics;
using Raylib_CSharp.Transformations;
using TinySynth.UI;

namespace TinySynth.App.Input;

internal readonly record struct InputDeviceContext(
    Vector2 MousePosition,
    bool MousePressed,
    bool MouseDown,
    bool MouseReleased,
    Rectangle HoldPedalBounds,
    PianoKeyLayout[] Keys);
