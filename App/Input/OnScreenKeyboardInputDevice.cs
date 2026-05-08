using TinySynth.UI;

namespace TinySynth.App.Input;

internal sealed class OnScreenKeyboardInputDevice : IInputDevice
{
    private int _activePointerMidiNote = -1;

    public void Update(InputDeviceContext context, ICollection<InputAction> actions)
    {
        if (context.MousePressed && Contains(context.HoldPedalBounds, context.MousePosition))
        {
            actions.Add(new InputAction(InputActionType.HoldPedalToggle));
        }

        int hoveredMidiNote = KeyboardLayoutBuilder.GetHoveredMidiNote(context.Keys, context.MousePosition);

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
            actions.Add(new InputAction(InputActionType.NoteActive, _activePointerMidiNote));
        }
    }

    private static bool Contains(Raylib_CSharp.Transformations.Rectangle bounds, System.Numerics.Vector2 point)
    {
        return point.X >= bounds.X
            && point.X <= bounds.X + bounds.Width
            && point.Y >= bounds.Y
            && point.Y <= bounds.Y + bounds.Height;
    }
}
