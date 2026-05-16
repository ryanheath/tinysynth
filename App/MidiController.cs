using Raylib_CSharp.Colors;
using Raylib_CSharp.Rendering;
using TinySynth.App.Input;

namespace TinySynth.App;

internal sealed class MidiController(IReadOnlyList<IInputDevice> inputDevices) : IDisposable
{
    private readonly MidiInputDevice? _midiInputDevice = inputDevices.OfType<MidiInputDevice>().FirstOrDefault();

    public string? Status => _midiInputDevice?.Status;

    public void DrawStatus(int x, int y, Color textColor)
    {
        if (Status is string midiStatus)
        {
            Graphics.DrawText(midiStatus, x, y, 18, textColor);
        }
    }

    public void Dispose()
    {
        _midiInputDevice?.Dispose();
    }
}
