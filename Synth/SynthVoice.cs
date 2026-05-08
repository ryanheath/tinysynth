namespace TinySynth.Synth;

internal sealed class SynthVoice
{
    private readonly int _sampleRate;
    private float _masterGain;
    private readonly OscillatorState[] _oscillators;

    private int _activeOscillatorCount;
    private float _filterLfoPhase;
    private float _filterInput1;
    private float _filterInput2;
    private float _filterOutput1;
    private float _filterOutput2;

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
            _filterLfoPhase = 0f;
            ResetFilterState();
        }

        EnvelopeStage = HasEnabledOscillator(parameters) ? EnvelopeStage.Attack : EnvelopeStage.Idle;

        if (EnvelopeStage == EnvelopeStage.Idle)
        {
            ActiveMidiNote = -1;
        }
    }

    public void ReleaseNote()
    {
        if (ActiveMidiNote < 0 || EnvelopeStage == EnvelopeStage.Idle)
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

    public void AddToBuffer(float[] audioBuffer, SynthParameters parameters)
    {
        for (int i = 0; i < audioBuffer.Length; i++)
        {
            audioBuffer[i] += NextSample(parameters);
        }
    }

    private float NextSample(SynthParameters parameters)
    {
        if (EnvelopeStage == EnvelopeStage.Idle || ActiveMidiNote < 0)
        {
            return 0f;
        }

        float deltaTime = 1f / _sampleRate;
        float sample = 0f;
        int enabledOscillatorCount = 0;

        for (int i = 0; i < _activeOscillatorCount; i++)
        {
            OscillatorParameters oscillatorParameters = parameters.GetOscillator(i);
            if (!oscillatorParameters.Enabled)
            {
                continue;
            }

            enabledOscillatorCount++;

            OscillatorState oscillator = _oscillators[i];

            UpdateEnvelope(deltaTime, oscillator, oscillatorParameters);
            UpdateFrequency(deltaTime, oscillator, oscillatorParameters);

            if (oscillator.EnvelopeLevel <= 0f && EnvelopeStage == EnvelopeStage.Release)
            {
                continue;
            }

            float effectiveFrequency = ApplyVibrato(oscillator.CurrentFrequency, deltaTime, oscillator, oscillatorParameters);
            float oscillatorSample = GetWaveSample(oscillator, oscillatorParameters, deltaTime);

            sample += oscillatorSample * oscillatorParameters.Gain * oscillator.EnvelopeLevel;
            AdvancePhase(oscillator, effectiveFrequency);
        }

        RefreshEnvelopeStage(parameters);

        if (EnvelopeStage == EnvelopeStage.Idle || enabledOscillatorCount == 0)
        {
            return 0f;
        }

        sample /= MathF.Sqrt(enabledOscillatorCount);
        sample = ProcessFilter(sample, parameters, deltaTime);

        return sample * _masterGain;
    }

    private float ProcessFilter(float inputSample, SynthParameters parameters, float deltaTime)
    {
        if (parameters.FilterType == FilterType.Off)
        {
            return inputSample;
        }

        float cutoffHz = GetModulatedFilterCutoffHz(parameters, deltaTime);
        float resonance = Math.Clamp(parameters.FilterResonance, 0f, 1f);
        float q = 0.707f + ((8f - 0.707f) * resonance * resonance);
        float omega = MathF.Tau * cutoffHz / _sampleRate;
        float sinOmega = MathF.Sin(omega);
        float cosOmega = MathF.Cos(omega);
        float alpha = sinOmega / (2f * q);

        float b0;
        float b1;
        float b2;
        float a0 = 1f + alpha;
        float a1 = -2f * cosOmega;
        float a2 = 1f - alpha;

        switch (parameters.FilterType)
        {
            case FilterType.LowPass:
                b0 = (1f - cosOmega) * 0.5f;
                b1 = 1f - cosOmega;
                b2 = (1f - cosOmega) * 0.5f;
                break;

            case FilterType.HighPass:
                b0 = (1f + cosOmega) * 0.5f;
                b1 = -(1f + cosOmega);
                b2 = (1f + cosOmega) * 0.5f;
                break;

            case FilterType.BandPass:
                b0 = alpha;
                b1 = 0f;
                b2 = -alpha;
                break;

            default:
                return inputSample;
        }

        float normalizedB0 = b0 / a0;
        float normalizedB1 = b1 / a0;
        float normalizedB2 = b2 / a0;
        float normalizedA1 = a1 / a0;
        float normalizedA2 = a2 / a0;

        float outputSample = (normalizedB0 * inputSample)
            + (normalizedB1 * _filterInput1)
            + (normalizedB2 * _filterInput2)
            - (normalizedA1 * _filterOutput1)
            - (normalizedA2 * _filterOutput2);

        _filterInput2 = _filterInput1;
        _filterInput1 = inputSample;
        _filterOutput2 = _filterOutput1;
        _filterOutput1 = outputSample;

        return outputSample;
    }

    private float GetModulatedFilterCutoffHz(SynthParameters parameters, float deltaTime)
    {
        float baseCutoffHz = Math.Clamp(parameters.FilterCutoffHz, 20f, _sampleRate * 0.45f);
        float envelopeLevel = GetAverageEnvelopeLevel();
        float envelopeOctaves = parameters.FilterEnvelopeAmount * envelopeLevel;

        if (parameters.FilterLfoRateHz > 0f)
        {
            _filterLfoPhase += parameters.FilterLfoRateHz * deltaTime;
            _filterLfoPhase -= MathF.Floor(_filterLfoPhase);
        }

        float lfoValue = MathF.Sin(_filterLfoPhase * MathF.Tau);
        float lfoOctaves = parameters.FilterLfoDepth * lfoValue;
        float modulatedCutoffHz = baseCutoffHz * MathF.Pow(2f, envelopeOctaves + lfoOctaves);

        return Math.Clamp(modulatedCutoffHz, 20f, _sampleRate * 0.45f);
    }

    private float GetAverageEnvelopeLevel()
    {
        float totalEnvelope = 0f;
        int enabledOscillatorCount = 0;

        for (int i = 0; i < _activeOscillatorCount; i++)
        {
            totalEnvelope += _oscillators[i].EnvelopeLevel;
            enabledOscillatorCount++;
        }

        return enabledOscillatorCount == 0
            ? 0f
            : totalEnvelope / enabledOscillatorCount;
    }

    private void ResetFilterState()
    {
        _filterInput1 = 0f;
        _filterInput2 = 0f;
        _filterOutput1 = 0f;
        _filterOutput2 = 0f;
    }

    private void UpdateFrequency(float deltaTime, OscillatorState oscillator, OscillatorParameters parameters)
    {
        if (ActiveMidiNote >= 0)
        {
            oscillator.TargetFrequency = ApplyDetune(MidiUtilities.MidiToFrequency(ActiveMidiNote), parameters.DetuneCents);
        }

        if (parameters.GlideSeconds <= 0.001f)
        {
            oscillator.CurrentFrequency = oscillator.TargetFrequency;
            return;
        }

        float glideFactor = MathF.Min(deltaTime / parameters.GlideSeconds, 1f);
        oscillator.CurrentFrequency += (oscillator.TargetFrequency - oscillator.CurrentFrequency) * glideFactor;
    }

    private void UpdateEnvelope(float deltaTime, OscillatorState oscillator, OscillatorParameters parameters)
    {
        switch (EnvelopeStage)
        {
            case EnvelopeStage.Idle:
                oscillator.EnvelopeLevel = 0f;
                break;

            case EnvelopeStage.Attack:
                oscillator.EnvelopeLevel += deltaTime / MathF.Max(parameters.AttackSeconds, 0.0001f);
                oscillator.EnvelopeLevel = MathF.Min(oscillator.EnvelopeLevel, 1f);
                break;

            case EnvelopeStage.Decay:
                if (parameters.DecaySeconds <= 0.01f)
                {
                    oscillator.EnvelopeLevel = parameters.SustainLevel;
                }
                else
                {
                    oscillator.EnvelopeLevel -= ((1f - parameters.SustainLevel) / parameters.DecaySeconds) * deltaTime;
                    oscillator.EnvelopeLevel = MathF.Max(oscillator.EnvelopeLevel, parameters.SustainLevel);
                }
                break;

            case EnvelopeStage.Sustain:
                oscillator.EnvelopeLevel = parameters.SustainLevel;
                break;

            case EnvelopeStage.Release:
                oscillator.ReleaseElapsed += deltaTime;

                if (parameters.ReleaseSeconds <= 0.01f || oscillator.ReleaseElapsed >= parameters.ReleaseSeconds)
                {
                    oscillator.EnvelopeLevel = 0f;
                }
                else
                {
                    float releaseProgress = oscillator.ReleaseElapsed / parameters.ReleaseSeconds;
                    oscillator.EnvelopeLevel = oscillator.ReleaseStartLevel * (1f - releaseProgress);
                }

                break;
        }
    }

    private void RefreshEnvelopeStage(SynthParameters parameters)
    {
        if (!HasEnabledOscillator(parameters))
        {
            ActiveMidiNote = -1;
            EnvelopeStage = EnvelopeStage.Idle;
            return;
        }

        if (EnvelopeStage == EnvelopeStage.Release)
        {
            if (AreAllOscillatorsAtOrBelow(0f))
            {
                ActiveMidiNote = -1;
                EnvelopeStage = EnvelopeStage.Idle;
            }

            return;
        }

        if (EnvelopeStage == EnvelopeStage.Attack)
        {
            if (AreAllOscillatorsAtOrAbove(1f))
            {
                EnvelopeStage = EnvelopeStage.Decay;
            }

            return;
        }

        if (EnvelopeStage != EnvelopeStage.Decay)
        {
            return;
        }

        for (int i = 0; i < _activeOscillatorCount; i++)
        {
            if (_oscillators[i].EnvelopeLevel > parameters.GetOscillator(i).SustainLevel)
            {
                return;
            }
        }

        EnvelopeStage = EnvelopeStage.Sustain;
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

    private bool AreAllOscillatorsAtOrBelow(float value)
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

    private bool AreAllOscillatorsAtOrAbove(float value)
    {
        for (int i = 0; i < _activeOscillatorCount; i++)
        {
            if (_oscillators[i].EnvelopeLevel < value)
            {
                return false;
            }
        }

        return true;
    }

    private float ApplyVibrato(float baseFrequency, float deltaTime, OscillatorState oscillator, OscillatorParameters parameters)
    {
        if (parameters.VibratoDepthCents <= 0f || parameters.VibratoRateHz <= 0f)
        {
            return baseFrequency;
        }

        oscillator.VibratoPhase += parameters.VibratoRateHz * deltaTime;
        oscillator.VibratoPhase -= MathF.Floor(oscillator.VibratoPhase);

        float vibratoCents = MathF.Sin(oscillator.VibratoPhase * MathF.Tau) * parameters.VibratoDepthCents;
        return ApplyDetune(baseFrequency, vibratoCents);
    }

    private static float GetWaveSample(OscillatorState oscillator, OscillatorParameters parameters, float deltaTime)
    {
        float phase = oscillator.Phase;
        return parameters.Waveform switch
        {
            Waveform.Sine => MathF.Sin(phase * MathF.Tau),
            Waveform.Square => GetPulseSample(oscillator, parameters, deltaTime),
            Waveform.Saw => (2f * phase) - 1f,
            Waveform.Triangle => 1f - (4f * MathF.Abs(phase - 0.5f)),
            Waveform.Noise => Random.Shared.NextSingle() * 2f - 1f,
            _ => 0f
        };
    }

    private static float GetPulseSample(OscillatorState oscillator, OscillatorParameters parameters, float deltaTime)
    {
        float pulseWidth = Math.Clamp(parameters.PulseWidth, 0.10f, 0.90f);

        if (parameters.PwmRateHz > 0.01f)
        {
            oscillator.PwmPhase += parameters.PwmRateHz * deltaTime;
            oscillator.PwmPhase -= MathF.Floor(oscillator.PwmPhase);
            float pwmDepth = MathF.Min(MathF.Abs(pulseWidth - 0.5f) + 0.15f, 0.40f);
            pulseWidth = Math.Clamp(pulseWidth + (MathF.Sin(oscillator.PwmPhase * MathF.Tau) * pwmDepth), 0.10f, 0.90f);
        }

        return oscillator.Phase < pulseWidth ? 1f : -1f;
    }

    private void AdvancePhase(OscillatorState oscillator, float frequency)
    {
        oscillator.Phase += frequency / _sampleRate;
        oscillator.Phase -= MathF.Floor(oscillator.Phase);
    }

    private static float ApplyDetune(float frequency, float cents)
    {
        return frequency * MathF.Pow(2f, cents / 1200f);
    }

    private sealed class OscillatorState
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
