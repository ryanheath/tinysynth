using TinySynth.Synth;

namespace TinySynth.App.Input;

internal sealed class InputActionProcessor
{
    private readonly HashSet<int> _currentPhysicalMidiNotes = [];
    private readonly HashSet<int> _previousPhysicalMidiNotes = [];
    private readonly Dictionary<int, int> _currentNoteVelocities = [];
    private readonly List<int> _noteChangeBuffer = [];

    public bool HoldPedalEnabled { get; private set; }

    public void ApplyActions(IReadOnlyCollection<InputAction> inputActions, SynthEngine synthEngine, SynthParameters synthParameters)
    {
        _currentPhysicalMidiNotes.Clear();
        _currentNoteVelocities.Clear();

        foreach (InputAction inputAction in inputActions)
        {
            switch (inputAction)
            {
                case NoteActiveInputAction { MidiNote: int midiNote, Velocity: int velocity }:
                    _currentPhysicalMidiNotes.Add(midiNote);
                    _currentNoteVelocities[midiNote] = velocity;
                    break;

                case HoldPedalSetInputAction { IsEnabled: bool isEnabled } when HoldPedalEnabled != isEnabled:
                    HoldPedalEnabled = isEnabled;
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
            int velocity = _currentNoteVelocities.GetValueOrDefault(note, 127);
            synthEngine.NoteOn(note, velocity, synthParameters);
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
