using Raylib_CSharp.Interact;
using RaylibInput = Raylib_CSharp.Interact.Input;

namespace TinySynth.App.Input;

internal sealed class ComputerKeyboardInputDevice : IInputDevice
{
    private const int DefaultVelocity = 127;

    private static readonly (KeyboardKey Key, int MidiNote)[] KeyboardMappings =
    [
        (KeyboardKey.Z, 60),
        (KeyboardKey.S, 61),
        (KeyboardKey.X, 62),
        (KeyboardKey.D, 63),
        (KeyboardKey.C, 64),
        (KeyboardKey.V, 65),
        (KeyboardKey.G, 66),
        (KeyboardKey.B, 67),
        (KeyboardKey.H, 68),
        (KeyboardKey.N, 69),
        (KeyboardKey.J, 70),
        (KeyboardKey.M, 71),
        (KeyboardKey.Comma, 72)
    ];

    public void Update(InputDeviceContext context, ICollection<InputAction> actions)
    {
        if (RaylibInput.IsKeyPressed(KeyboardKey.Space))
        {
            actions.Add(new HoldPedalSetInputAction(!context.HoldPedalEnabled));
        }

        foreach ((KeyboardKey key, int midiNote) in KeyboardMappings)
        {
            if (RaylibInput.IsKeyDown(key))
            {
                actions.Add(new NoteActiveInputAction(midiNote, DefaultVelocity));
            }
        }
    }
}
