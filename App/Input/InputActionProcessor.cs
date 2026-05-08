using TinySynth.Synth;

namespace TinySynth.App.Input;

internal sealed class InputActionProcessor
{
    private readonly HashSet<int> _currentPhysicalMidiNotes = [];
    private readonly HashSet<int> _previousPhysicalMidiNotes = [];
    private readonly List<int> _noteChangeBuffer = [];

    public bool HoldPedalEnabled { get; private set; }

    public void ApplyActions(IReadOnlyCollection<InputAction> inputActions, SynthEngine synthEngine, SynthParameters synthParameters)
    {
        _currentPhysicalMidiNotes.Clear();

        foreach (InputAction inputAction in inputActions)
        {
            switch (inputAction.Type)
            {
                case InputActionType.NoteActive when inputAction.MidiNote is int midiNote:
                    _currentPhysicalMidiNotes.Add(midiNote);
                    break;

                case InputActionType.HoldPedalToggle:
                    HoldPedalEnabled = !HoldPedalEnabled;
                    synthEngine.SetHoldPedal(HoldPedalEnabled);
                    break;
            }
        }

        SyncPhysicalNotes(synthEngine, synthParameters);
    }

    private void SyncPhysicalNotes(SynthEngine synthEngine, SynthParameters synthParameters)
    {
        _noteChangeBuffer.Clear();

        foreach (int note in _currentPhysicalMidiNotes)
        {
            if (!_previousPhysicalMidiNotes.Contains(note))
            {
                _noteChangeBuffer.Add(note);
            }
        }

        foreach (int note in _noteChangeBuffer)
        {
            synthEngine.NoteOn(note, synthParameters);
        }

        _noteChangeBuffer.Clear();

        foreach (int note in _previousPhysicalMidiNotes)
        {
            if (!_currentPhysicalMidiNotes.Contains(note))
            {
                _noteChangeBuffer.Add(note);
            }
        }

        foreach (int note in _noteChangeBuffer)
        {
            synthEngine.NoteOff(note);
        }

        _previousPhysicalMidiNotes.Clear();

        foreach (int note in _currentPhysicalMidiNotes)
        {
            _previousPhysicalMidiNotes.Add(note);
        }
    }
}
