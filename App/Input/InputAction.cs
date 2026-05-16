namespace TinySynth.App.Input;

internal abstract record class InputAction;

internal sealed record class NoteActiveInputAction(int MidiNote, int Velocity) : InputAction;

internal sealed record class HoldPedalSetInputAction(bool IsEnabled) : InputAction;
