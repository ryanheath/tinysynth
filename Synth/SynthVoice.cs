using TinySynth.Synth.Modulation;
using TinySynth.Synth.Snapshots;

namespace TinySynth.Synth;

internal sealed class SynthVoice
{
    private const int StereoChannelCount = 2;

    private readonly int _sampleRate;
    private float _masterGain;
    private readonly OscillatorState[] _oscillators;

    private int _activeOscillatorCount;
    private bool _isOneShotVoice;
    private float _modulationLfoPhase1;
    private float _modulationLfoPhase2;
    private float _currentAverageEnvelopeLevel;
    private readonly float[] _filterInput1 = new float[StereoChannelCount];
    private readonly float[] _filterInput2 = new float[StereoChannelCount];
    private readonly float[] _filterOutput1 = new float[StereoChannelCount];
    private readonly float[] _filterOutput2 = new float[StereoChannelCount];
    private float[] _filterCutoffBuffer = [];
    private float[] _filterResonanceBuffer = [];

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

    public float ModulationEnvelopeLevel => _currentAverageEnvelopeLevel;

    public int ActiveMidiNote { get; private set; } = -1;

    internal int SampleRate => _sampleRate;

    internal float MasterGain => _masterGain;

    internal int ActiveOscillatorCount => _activeOscillatorCount;

    internal float ModulationLfoPhase1
    {
        get => _modulationLfoPhase1;
        set => _modulationLfoPhase1 = value;
    }

    internal float ModulationLfoPhase2
    {
        get => _modulationLfoPhase2;
        set => _modulationLfoPhase2 = value;
    }

    internal float CurrentAverageEnvelopeLevel
    {
        get => _currentAverageEnvelopeLevel;
        set => _currentAverageEnvelopeLevel = value;
    }

    internal float[] FilterInput1 => _filterInput1;

    internal float[] FilterInput2 => _filterInput2;

    internal float[] FilterOutput1 => _filterOutput1;

    internal float[] FilterOutput2 => _filterOutput2;

    internal float[] FilterCutoffBuffer => _filterCutoffBuffer;

    internal float[] FilterResonanceBuffer => _filterResonanceBuffer;

    public void SetMasterGain(float masterGain)
    {
        _masterGain = masterGain;
    }

    public void StartNote(int midiNote, SynthParameters parameters, bool forceRestart = false)
    {
        bool isAudible = EnvelopeStage != EnvelopeStage.Idle && HasAudibleOscillator();
        float noteFrequency = MidiUtilities.MidiToFrequency(midiNote);

        ActiveMidiNote = midiNote;
        _activeOscillatorCount = parameters.Oscillators.Count;
        _isOneShotVoice = HasOnlyOneShotOscillators(parameters);

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

            oscillator.TargetFrequency = ApplyDetune(noteFrequency, oscillatorParameters.DetuneCents);

            if (!isAudible || forceRestart)
            {
                oscillator.Phase = 0f;
                oscillator.VibratoPhase = 0f;
                oscillator.PwmPhase = 0f;
                oscillator.EnvelopeLevel = 0f;
                oscillator.CurrentFrequency = oscillator.TargetFrequency;
            }

            oscillator.ReleaseElapsed = 0f;
            oscillator.ReleaseStartLevel = 0f;
        }

        if (!isAudible || forceRestart)
        {
            _modulationLfoPhase1 = 0f;
            _modulationLfoPhase2 = 0f;
            ResetFilterState();
        }

        EnvelopeStage = HasEnabledOscillator(parameters) ? EnvelopeStage.Attack : EnvelopeStage.Idle;

        if (EnvelopeStage == EnvelopeStage.Idle)
        {
            ActiveMidiNote = -1;
            _currentAverageEnvelopeLevel = 0f;
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
    }

    internal void EnsureStageBuffers(int frameCount)
    {
        if (_filterCutoffBuffer.Length != frameCount)
        {
            _filterCutoffBuffer = new float[frameCount];
            _filterResonanceBuffer = new float[frameCount];
        }
    }

    internal int GetEnabledOscillatorCount(SynthPatchSnapshot patchSnapshot)
    {
        int count = 0;

        for (int i = 0; i < _activeOscillatorCount; i++)
        {
            if (patchSnapshot.GetOscillator(i).Enabled)
            {
                count++;
            }
        }

        return count;
    }

    internal float GetModulatedFilterCutoffHz(SynthPatchSnapshot patchSnapshot, VoiceModulationState modulationState)
    {
        float baseCutoffHz = Math.Clamp(patchSnapshot.FilterCutoffHz, 20f, _sampleRate * 0.45f);
        float matrixOctaves = modulationState.FilterCutoff;
        float modulatedCutoffHz = baseCutoffHz * MathF.Pow(2f, matrixOctaves);

        return Math.Clamp(modulatedCutoffHz, 20f, _sampleRate * 0.45f);
    }

    internal float GetKeyTrackValue()
    {
        if (ActiveMidiNote < 0)
        {
            return 0f;
        }

        return Math.Clamp((ActiveMidiNote - 60f) / 24f, -1f, 1f);
    }

    internal float GetAverageEnvelopeLevel(SynthPatchSnapshot patchSnapshot)
    {
        float totalEnvelope = 0f;
        int enabledOscillatorCount = 0;

        for (int i = 0; i < _activeOscillatorCount; i++)
        {
            if (!patchSnapshot.GetOscillator(i).Enabled)
            {
                continue;
            }

            totalEnvelope += _oscillators[i].EnvelopeLevel;
            enabledOscillatorCount++;
        }

        return enabledOscillatorCount == 0
            ? 0f
            : totalEnvelope / enabledOscillatorCount;
    }

    internal void ResetFilterState()
    {
        Array.Clear(_filterInput1);
        Array.Clear(_filterInput2);
        Array.Clear(_filterOutput1);
        Array.Clear(_filterOutput2);
    }

    private static (float Left, float Right) GetPanGains(float pan)
    {
        float normalizedPan = Math.Clamp(pan, -1f, 1f);
        float angle = (normalizedPan + 1f) * (MathF.PI * 0.25f);
        return (MathF.Cos(angle), MathF.Sin(angle));
    }

    private bool HasAudibleOscillator()
    {
        for (int i = 0; i < _activeOscillatorCount; i++)
        {
            if (_oscillators[i].EnvelopeLevel > 0f)
            {
                return true;
            }
        }

        return false;
    }

    internal bool HasEnabledOscillator(SynthPatchSnapshot patchSnapshot)
    {
        for (int i = 0; i < _activeOscillatorCount; i++)
        {
            if (patchSnapshot.GetOscillator(i).Enabled)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasOnlyOneShotOscillators(SynthParameters parameters)
    {
        bool hasEnabledOscillator = false;

        for (int i = 0; i < _activeOscillatorCount; i++)
        {
            OscillatorParameters oscillatorParameters = parameters.GetOscillator(i);
            if (!oscillatorParameters.Enabled)
            {
                continue;
            }

            hasEnabledOscillator = true;

            if (oscillatorParameters.EnvelopeMode != EnvelopeMode.OneShot)
            {
                return false;
            }
        }

        return hasEnabledOscillator;
    }

    private bool HasEnabledOscillator(SynthParameters parameters)
    {
        for (int i = 0; i < _activeOscillatorCount; i++)
        {
            if (parameters.GetOscillator(i).Enabled)
            {
                return true;
            }
        }

        return false;
    }

    internal bool AreAllOscillatorsAtOrBelow(float value)
    {
        for (int i = 0; i < _activeOscillatorCount; i++)
        {
            if (_oscillators[i].EnvelopeLevel > value)
            {
                return false;
            }
        }

        return true;
    }

    internal bool AreAllEnabledOscillatorsAtOrAbove(SynthPatchSnapshot patchSnapshot, float value)
    {
        bool hasEnabledOscillator = false;

        for (int i = 0; i < _activeOscillatorCount; i++)
        {
            if (!patchSnapshot.GetOscillator(i).Enabled)
            {
                continue;
            }

            hasEnabledOscillator = true;

            if (_oscillators[i].EnvelopeLevel < value)
            {
                return false;
            }
        }

        return hasEnabledOscillator;
    }

    private static float ApplyDetune(float frequency, float cents)
    {
        return frequency * MathF.Pow(2f, cents / 1200f);
    }

    internal sealed class OscillatorState
    {
        public OscillatorState(float defaultFrequency)
        {
            CurrentFrequency = defaultFrequency;
            TargetFrequency = defaultFrequency;
        }

        public float CurrentFrequency { get; set; }

        public float TargetFrequency { get; set; }

        public float Phase { get; set; }

        public float VibratoPhase { get; set; }

        public float PwmPhase { get; set; }

        public float EnvelopeLevel { get; set; }

        public float ReleaseStartLevel { get; set; }

        public float ReleaseElapsed { get; set; }
    }
}
