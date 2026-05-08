using Raylib_CSharp.Transformations;
using TinySynth.UI;

namespace TinySynth.App.Input;

internal interface IOnScreenInputDevice : IInputDevice
{
    void SetLayout(PianoKeyLayout[] keys, Rectangle holdPedalBounds);
}
