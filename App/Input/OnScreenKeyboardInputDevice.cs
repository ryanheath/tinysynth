using Raylib_CSharp.Transformations;
using TinySynth.UI;

namespace TinySynth.App.Input;

internal sealed class OnScreenKeyboardInputDevice : IOnScreenInputDevice
{
    private const int DefaultVelocity = 127;

    private int _activePointerMidiNote = -1;
    private PianoKeyLayout[] _keys = [];
    private Rectangle _holdPedalBounds;

    public void SetLayout(PianoKeyLayout[] keys, Rectangle holdPedalBounds)
    {
        _keys = keys;
        _holdPedalBounds = holdPedalBounds;
    }

    public void Update(InputDeviceContext context, ICollection<InputAction> actions)
    {
        if (context.MousePressed && UiHitTesting.Contains(_holdPedalBounds, context.MousePosition))
        {
            actions.Add(new HoldPedalSetInputAction(!context.HoldPedalEnabled));
        }

        int hoveredMidiNote = KeyboardLayoutBuilder.GetHoveredMidiNote(_keys, context.MousePosition);

        if (context.MousePressed && hoveredMidiNote >= 0)
        {
            _activePointerMidiNote = hoveredMidiNote;
        }
        else if (context.MouseDown && _activePointerMidiNote >= 0 && hoveredMidiNote >= 0)
        {
            _activePointerMidiNote = hoveredMidiNote;
        }
        else if (context.MouseReleased)
        {
            _activePointerMidiNote = -1;
        }

        if (_activePointerMidiNote >= 0)
        {
            actions.Add(new NoteActiveInputAction(_activePointerMidiNote, DefaultVelocity));
        }
    }
}
