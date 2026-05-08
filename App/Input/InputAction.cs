namespace TinySynth.App.Input;

internal readonly record struct InputAction(
    InputActionType Type,
    int? MidiNote = null);
