using Raylib_CSharp.Transformations;
using System.Numerics;

namespace TinySynth.UI;

internal static class UiHitTesting
{
    public static bool Contains(Rectangle bounds, Vector2 point)
    {
        return point.X >= bounds.X
            && point.X <= bounds.X + bounds.Width
            && point.Y >= bounds.Y
            && point.Y <= bounds.Y + bounds.Height;
    }
}
