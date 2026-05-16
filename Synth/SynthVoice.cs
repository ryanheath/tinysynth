using TinySynth.Synth.Dsp;
using TinySynth.Synth.Snapshots;

namespace TinySynth.Synth;

internal sealed class SynthVoice
{
    private readonly int _sampleRate;
    private float _masterGain;
    private readonly OscillatorState[] _oscillators;

    private int _activeOscillatorCount;
    private bool _isOneShotVoice;

    public SynthVoice(int sampleRate, float masterGain, int defaultMidiNote)
    {
        _sampleRate = sampleRate;
        _masterGain = masterGain;
        _oscillators = new OscillatorState[SynthParameters.OscillatorCount];

        float defaultFrequency = MidiUtilities.MidiToFrequency(defaultMidiNote);

        for (int i = 0; i < _oscillators.Length; i++)
        {
            _oscillators[i] = new OscillatorState(defaultFrequency);
        }
    }

    public EnvelopeStage EnvelopeStage { get; private set; } = EnvelopeStage.Idle;

    public bool IsIdle => EnvelopeStage == EnvelopeStage.Idle;

    public float CurrentFrequency => _activeOscillatorCount == 0 ? 0f : _oscillators[0].CurrentFrequency;

    public int ActiveMidiNote { get; private set; } = -1;

    internal float NoteVelocity { get; private set; } = 1f;

    internal int SampleRate => _sampleRate;

    internal float MasterGain => _masterGain;

    internal int ActiveOscillatorCount => _activeOscillatorCount;

    public void SetMasterGain(float masterGain)
    {
        _masterGain = masterGain;
    }

    public void StartNote(int midiNote, int velocity, SynthParameters parameters, bool forceRestart = false)
    {
        bool isAudible = EnvelopeStage != EnvelopeStage.Idle && VoiceRuntimeInspector.HasAudibleOscillator(this);
        float noteFrequency = MidiUtilities.MidiToFrequency(midiNote);

        ActiveMidiNote = midiNote;
        NoteVelocity = Math.Clamp(velocity, 0, 127) / 127f;
        _activeOscillatorCount = parameters.Oscillators.Count;
        _isOneShotVoice = VoiceRuntimeInspector.HasOnlyOneShotOscillators(this, parameters);

        for (int i = 0; i < _activeOscillatorCount; i++)
        {
            OscillatorParameters oscillatorParameters = parameters.GetOscillator(i);
            OscillatorState oscillator = _oscillators[i];

            if (!oscillatorParameters.Enabled)
            {
                oscillator.EnvelopeLevel = 0f;
                oscillator.ReleaseElapsed = 0f;
                oscillator.ReleaseStartLevel = 0f;
                continue;
            }

            oscillator.TargetFrequency = PitchMath.ApplyDetune(noteFrequency, oscillatorParameters.DetuneCents);

            if (!isAudible || forceRestart)
            {
                oscillator.Phase = 0f;
                oscillator.SuperSawPhaseA = 0f;
                oscillator.SuperSawPhaseB = 0f;
                oscillator.SuperSawPhaseC = 0f;
                oscillator.VibratoPhase = 0f;
                oscillator.PwmPhase = 0f;
                oscillator.EnvelopeLevel = 0f;
                oscillator.CurrentFrequency = oscillator.TargetFrequency;
            }

            oscillator.ReleaseElapsed = 0f;
            oscillator.ReleaseStartLevel = 0f;
        }

        EnvelopeStage = VoiceRuntimeInspector.HasEnabledOscillator(this, parameters) ? EnvelopeStage.Attack : EnvelopeStage.Idle;

        if (EnvelopeStage == EnvelopeStage.Idle)
        {
            ActiveMidiNote = -1;
            NoteVelocity = 1f;
        }
    }

    public void ReleaseNote()
    {
        if (ActiveMidiNote < 0 || EnvelopeStage == EnvelopeStage.Idle)
        {
            return;
        }

        if (_isOneShotVoice)
        {
            return;
        }

        for (int i = 0; i < _activeOscillatorCount; i++)
        {
            OscillatorState oscillator = _oscillators[i];
            oscillator.ReleaseStartLevel = oscillator.EnvelopeLevel;
            oscillator.ReleaseElapsed = 0f;
        }

        EnvelopeStage = EnvelopeStage.Release;
    }

    internal OscillatorState GetOscillatorState(int index)
    {
        return _oscillators[index];
    }

    internal void SetEnvelopeStageState(int activeMidiNote, EnvelopeStage envelopeStage)
    {
        ActiveMidiNote = activeMidiNote;
        EnvelopeStage = envelopeStage;

        if (envelopeStage == EnvelopeStage.Idle)
        {
            NoteVelocity = 1f;
        }
    }

    internal sealed class OscillatorState(float defaultFrequency)
    {
        public float CurrentFrequency { get; set; } = defaultFrequency;

        public float TargetFrequency { get; set; } = defaultFrequency;

        public float Phase { get; set; }

        public float SuperSawPhaseA { get; set; }

        public float SuperSawPhaseB { get; set; }

        public float SuperSawPhaseC { get; set; }

        public float VibratoPhase { get; set; }

        public float PwmPhase { get; set; }

        public float EnvelopeLevel { get; set; }

        public float ReleaseStartLevel { get; set; }

        public float ReleaseElapsed { get; set; }
    }
}
