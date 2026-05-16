using NAudio.Midi;

namespace TinySynth.App.Input;

internal sealed class MidiInputDevice : IInputDevice, IDisposable
{
    private const int SustainControllerNumber = 64;

    private readonly Lock _sync = new();
    private readonly Dictionary<int, int> _activeNotes = [];
    private readonly MidiIn? _midiIn;

    private bool _holdPedalEnabled;
    private bool _holdPedalStateChanged;

    public MidiInputDevice()
    {
        if (!OperatingSystem.IsWindows())
        {
            Status = "MIDI unavailable";
            return;
        }

        if (MidiIn.NumberOfDevices <= 0)
        {
            Status = "MIDI: no device";
            return;
        }

        MidiInCapabilities deviceInfo = MidiIn.DeviceInfo(0);
        DeviceName = deviceInfo.ProductName;
        Status = $"MIDI: {DeviceName}";
        _midiIn = new MidiIn(0);
        _midiIn.MessageReceived += HandleMessageReceived;
        _midiIn.ErrorReceived += HandleErrorReceived;
        _midiIn.Start();
    }

    public string DeviceName { get; } = "None";

    public string Status { get; private set; }

    public void Update(InputDeviceContext context, ICollection<InputAction> actions)
    {
        lock (_sync)
        {
            foreach ((int midiNote, int velocity) in _activeNotes)
            {
                actions.Add(new NoteActiveInputAction(midiNote, velocity));
            }

            if (_holdPedalStateChanged)
            {
                actions.Add(new HoldPedalSetInputAction(_holdPedalEnabled));
                _holdPedalStateChanged = false;
            }
        }
    }

    public void Dispose()
    {
        if (_midiIn is null)
        {
            return;
        }

        _midiIn.Stop();
        _midiIn.MessageReceived -= HandleMessageReceived;
        _midiIn.ErrorReceived -= HandleErrorReceived;
        _midiIn.Dispose();
    }

    private void HandleMessageReceived(object? sender, MidiInMessageEventArgs e)
    {
        lock (_sync)
        {
            switch (e.MidiEvent.CommandCode)
            {
                case MidiCommandCode.NoteOn:
                    NoteEvent noteOnEvent = (NoteEvent)e.MidiEvent;
                    if (noteOnEvent.Velocity > 0)
                    {
                        _activeNotes[noteOnEvent.NoteNumber] = noteOnEvent.Velocity;
                    }
                    else
                    {
                        _activeNotes.Remove(noteOnEvent.NoteNumber);
                    }
                    break;

                case MidiCommandCode.NoteOff:
                    NoteEvent noteOffEvent = (NoteEvent)e.MidiEvent;
                    _activeNotes.Remove(noteOffEvent.NoteNumber);
                    break;

                case MidiCommandCode.ControlChange:
                    ControlChangeEvent controlChangeEvent = (ControlChangeEvent)e.MidiEvent;
                    if ((int)controlChangeEvent.Controller == SustainControllerNumber)
                    {
                        bool isEnabled = controlChangeEvent.ControllerValue >= 64;
                        if (_holdPedalEnabled != isEnabled)
                        {
                            _holdPedalEnabled = isEnabled;
                            _holdPedalStateChanged = true;
                        }
                    }
                    break;
            }
        }
    }

    private void HandleErrorReceived(object? sender, MidiInMessageEventArgs e)
    {
        lock (_sync)
        {
            Status = $"MIDI error: 0x{e.RawMessage:X8}";
        }
    }
}
