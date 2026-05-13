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

    public void AddToBuffer(float[] audioBuffer, SynthParameters parameters)
    {
        AddToBuffer(audioBuffer, SynthPatchSnapshot.Create(parameters));
    }

    public void AddToBuffer(float[] audioBuffer, SynthPatchSnapshot patchSnapshot)
    {
        for (int i = 0; i < audioBuffer.Length; i += StereoChannelCount)
        {
            (float left, float right) = NextSample(patchSnapshot);
            audioBuffer[i] += left;
            audioBuffer[i + 1] += right;
        }
    }

    private (float Left, float Right) NextSample(SynthPatchSnapshot patchSnapshot)
    {
        if (EnvelopeStage == EnvelopeStage.Idle || ActiveMidiNote < 0)
        {
            return (0f, 0f);
        }

        float deltaTime = 1f / _sampleRate;
        float leftSample = 0f;
        float rightSample = 0f;
        int enabledOscillatorCount = 0;

        for (int i = 0; i < _activeOscillatorCount; i++)
        {
            OscillatorSnapshot oscillatorParameters = patchSnapshot.GetOscillator(i);
            if (!oscillatorParameters.Enabled)
            {
                continue;
            }

            enabledOscillatorCount++;

            OscillatorState oscillator = _oscillators[i];

            UpdateEnvelope(deltaTime, oscillator, oscillatorParameters);
            UpdateFrequency(deltaTime, oscillator, oscillatorParameters);
        }

        float envelopeLevel = GetAverageEnvelopeLevel(patchSnapshot);
        _currentAverageEnvelopeLevel = envelopeLevel;
        float keyTrackValue = GetKeyTrackValue();
        ModulationSourceValues rateSourceValues = new(0f, 0f, envelopeLevel, keyTrackValue);
        VoiceModulationState lfoRateModulationState = patchSnapshot.ModulationMatrix.EvaluateVoice(rateSourceValues, oscillatorIndex: -1);
        float lfo1Rate = GetModulatedLfoRate(patchSnapshot.Lfo1.RateHz, lfoRateModulationState.Lfo1Rate);
        float lfo2Rate = GetModulatedLfoRate(patchSnapshot.Lfo2.RateHz, lfoRateModulationState.Lfo2Rate);
        float lfo1Value = GetModulationLfoValue(patchSnapshot.Lfo1, deltaTime, ref _modulationLfoPhase1, lfo1Rate);
        float lfo2Value = GetModulationLfoValue(patchSnapshot.Lfo2, deltaTime, ref _modulationLfoPhase2, lfo2Rate);
        ModulationSourceValues sourceValues = new(lfo1Value, lfo2Value, envelopeLevel, keyTrackValue);
        VoiceModulationState sharedModulationState = patchSnapshot.ModulationMatrix.EvaluateVoice(sourceValues, oscillatorIndex: -1);
        float filterCutoffHz = GetModulatedFilterCutoffHz(patchSnapshot, sharedModulationState);

        for (int i = 0; i < _activeOscillatorCount; i++)
        {
            OscillatorSnapshot oscillatorParameters = patchSnapshot.GetOscillator(i);
            if (!oscillatorParameters.Enabled)
            {
                continue;
            }

            OscillatorState oscillator = _oscillators[i];

            if (oscillator.EnvelopeLevel <= 0f && EnvelopeStage == EnvelopeStage.Release)
            {
                continue;
            }

            VoiceModulationState oscillatorModulationState = patchSnapshot.ModulationMatrix.EvaluateVoice(sourceValues, i);
            float effectiveFrequency = ApplyVibrato(oscillator.CurrentFrequency, deltaTime, oscillator, oscillatorParameters);
            effectiveFrequency = ApplyPitchModulation(effectiveFrequency, oscillatorModulationState.Pitch);
            float pulseWidthModulation = oscillatorModulationState.PulseWidth;
            float oscillatorSample = GetWaveSample(oscillator, oscillatorParameters, deltaTime, pulseWidthModulation);
            float gain = Math.Clamp(oscillatorParameters.Gain + oscillatorModulationState.Gain, 0f, 1.25f);
            float oscillatorLevel = oscillatorSample * gain * oscillator.EnvelopeLevel;
            float pan = Math.Clamp(oscillatorParameters.Pan + oscillatorModulationState.Pan, -1f, 1f);
            (float leftGain, float rightGain) = GetPanGains(pan);

            leftSample += oscillatorLevel * leftGain;
            rightSample += oscillatorLevel * rightGain;
            AdvancePhase(oscillator, effectiveFrequency);
        }

        RefreshEnvelopeStage(patchSnapshot);

        if (EnvelopeStage == EnvelopeStage.Idle || enabledOscillatorCount == 0)
        {
            _currentAverageEnvelopeLevel = 0f;
            return (0f, 0f);
        }

        float normalization = 1f / MathF.Sqrt(enabledOscillatorCount);
        leftSample = ProcessFilter(leftSample * normalization, patchSnapshot, sharedModulationState, filterCutoffHz, channelIndex: 0);
        rightSample = ProcessFilter(rightSample * normalization, patchSnapshot, sharedModulationState, filterCutoffHz, channelIndex: 1);

        return (leftSample * _masterGain, rightSample * _masterGain);
    }

    private float ProcessFilter(float inputSample, SynthPatchSnapshot patchSnapshot, VoiceModulationState modulationState, float cutoffHz, int channelIndex)
    {
        if (patchSnapshot.FilterType == FilterType.Off)
        {
            return inputSample;
        }

        float resonance = Math.Clamp(patchSnapshot.FilterResonance + modulationState.FilterResonance, 0f, 1f);
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

        switch (patchSnapshot.FilterType)
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
            + (normalizedB1 * _filterInput1[channelIndex])
            + (normalizedB2 * _filterInput2[channelIndex])
            - (normalizedA1 * _filterOutput1[channelIndex])
            - (normalizedA2 * _filterOutput2[channelIndex]);

        _filterInput2[channelIndex] = _filterInput1[channelIndex];
        _filterInput1[channelIndex] = inputSample;
        _filterOutput2[channelIndex] = _filterOutput1[channelIndex];
        _filterOutput1[channelIndex] = outputSample;

        return outputSample;
    }

    private float GetModulatedFilterCutoffHz(SynthPatchSnapshot patchSnapshot, VoiceModulationState modulationState)
    {
        float baseCutoffHz = Math.Clamp(patchSnapshot.FilterCutoffHz, 20f, _sampleRate * 0.45f);
        float matrixOctaves = modulationState.FilterCutoff;
        float modulatedCutoffHz = baseCutoffHz * MathF.Pow(2f, matrixOctaves);

        return Math.Clamp(modulatedCutoffHz, 20f, _sampleRate * 0.45f);
    }

    private float GetKeyTrackValue()
    {
        if (ActiveMidiNote < 0)
        {
            return 0f;
        }

        return Math.Clamp((ActiveMidiNote - 60f) / 24f, -1f, 1f);
    }

    private float GetAverageEnvelopeLevel(SynthPatchSnapshot patchSnapshot)
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

    private void ResetFilterState()
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

    private void UpdateFrequency(float deltaTime, OscillatorState oscillator, OscillatorSnapshot parameters)
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

    private void UpdateEnvelope(float deltaTime, OscillatorState oscillator, OscillatorSnapshot parameters)
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
                float decayTarget = parameters.EnvelopeMode == EnvelopeMode.OneShot
                    ? 0f
                    : parameters.SustainLevel;

                if (parameters.DecaySeconds <= 0.01f)
                {
                    oscillator.EnvelopeLevel = decayTarget;
                }
                else
                {
                    oscillator.EnvelopeLevel -= ((1f - decayTarget) / parameters.DecaySeconds) * deltaTime;
                    oscillator.EnvelopeLevel = MathF.Max(oscillator.EnvelopeLevel, decayTarget);
                }
                break;

            case EnvelopeStage.Sustain:
                oscillator.EnvelopeLevel = parameters.EnvelopeMode == EnvelopeMode.OneShot
                    ? 0f
                    : parameters.SustainLevel;
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

    private void RefreshEnvelopeStage(SynthPatchSnapshot patchSnapshot)
    {
        if (!HasEnabledOscillator(patchSnapshot))
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
            if (AreAllEnabledOscillatorsAtOrAbove(patchSnapshot, 1f))
            {
                EnvelopeStage = EnvelopeStage.Decay;
            }

            return;
        }

        if (EnvelopeStage != EnvelopeStage.Decay)
        {
            return;
        }

        bool hasSustainOscillator = false;

        for (int i = 0; i < _activeOscillatorCount; i++)
        {
            OscillatorSnapshot oscillatorParameters = patchSnapshot.GetOscillator(i);

            if (!oscillatorParameters.Enabled)
            {
                continue;
            }

            if (oscillatorParameters.EnvelopeMode == EnvelopeMode.OneShot)
            {
                if (_oscillators[i].EnvelopeLevel > 0f)
                {
                    return;
                }

                continue;
            }

            hasSustainOscillator = true;

            if (_oscillators[i].EnvelopeLevel > oscillatorParameters.SustainLevel)
            {
                return;
            }
        }

        if (!hasSustainOscillator)
        {
            ActiveMidiNote = -1;
            EnvelopeStage = EnvelopeStage.Idle;
            return;
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

    private bool HasEnabledOscillator(SynthPatchSnapshot patchSnapshot)
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

    private bool AreAllEnabledOscillatorsAtOrAbove(SynthPatchSnapshot patchSnapshot, float value)
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

    private float ApplyVibrato(float baseFrequency, float deltaTime, OscillatorState oscillator, OscillatorSnapshot parameters)
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

    private static float GetModulatedLfoRate(float baseRateHz, float rateModulation)
    {
        return Math.Clamp(baseRateHz + (rateModulation * 6f), 0.01f, 20f);
    }

    private static float GetModulationLfoValue(ModulationLfoSnapshot parameters, float deltaTime, ref float phase, float effectiveRateHz)
    {
        if (effectiveRateHz > 0f)
        {
            phase += effectiveRateHz * deltaTime;
            phase -= MathF.Floor(phase);
        }

        return parameters.Shape switch
        {
            ModulationLfoShape.Sine => MathF.Sin(phase * MathF.Tau),
            ModulationLfoShape.Triangle => 1f - (4f * MathF.Abs(phase - 0.5f)),
            ModulationLfoShape.Saw => (2f * phase) - 1f,
            ModulationLfoShape.Square => phase < 0.5f ? 1f : -1f,
            _ => 0f
        } * Math.Clamp(parameters.Depth, 0f, 1f);
    }

    private static float ApplyPitchModulation(float baseFrequency, float pitchModulation)
    {
        float pitchCents = pitchModulation * 120f;
        return ApplyDetune(baseFrequency, pitchCents);
    }

    private static float GetWaveSample(OscillatorState oscillator, OscillatorSnapshot parameters, float deltaTime, float pulseWidthModulation)
    {
        float phase = oscillator.Phase;
        return parameters.Waveform switch
        {
            Waveform.Sine => MathF.Sin(phase * MathF.Tau),
            Waveform.Square => GetSquareSample(phase),
            Waveform.Pulse => GetPulseSample(oscillator, parameters, deltaTime, pulseWidthModulation),
            Waveform.Saw => (2f * phase) - 1f,
            Waveform.Triangle => 1f - (4f * MathF.Abs(phase - 0.5f)),
            Waveform.Noise => Random.Shared.NextSingle() * 2f - 1f,
            Waveform.SuperSaw => GetSuperSawSample(phase),
            Waveform.Organ => GetOrganSample(phase),
            Waveform.Metallic => GetMetallicSample(phase),
            Waveform.PinkNoise => GetPinkNoiseSample(),
            _ => 0f
        };
    }

    private static float GetSquareSample(float phase)
    {
        return phase < 0.5f ? 1f : -1f;
    }

    private static float GetPulseSample(OscillatorState oscillator, OscillatorSnapshot parameters, float deltaTime, float pulseWidthModulation)
    {
        float pulseWidth = Math.Clamp(parameters.PulseWidth + (pulseWidthModulation * 0.45f), 0.10f, 0.90f);

        if (parameters.PwmRateHz > 0.01f)
        {
            oscillator.PwmPhase += parameters.PwmRateHz * deltaTime;
            oscillator.PwmPhase -= MathF.Floor(oscillator.PwmPhase);
            float pwmDepth = MathF.Min(MathF.Abs(pulseWidth - 0.5f) + 0.15f, 0.40f);
            pulseWidth = Math.Clamp(pulseWidth + (MathF.Sin(oscillator.PwmPhase * MathF.Tau) * pwmDepth), 0.10f, 0.90f);
        }

        return oscillator.Phase < pulseWidth ? 1f : -1f;
    }

    private static float GetSuperSawSample(float phase)
    {
        float detuneA = WrapPhase(phase + 0.0125f);
        float detuneB = WrapPhase(phase - 0.0175f);
        float detuneC = WrapPhase(phase + 0.031f);

        float sample = GetSawSample(phase);
        sample += GetSawSample(detuneA) * 0.8f;
        sample += GetSawSample(detuneB) * 0.7f;
        sample += GetSawSample(detuneC) * 0.55f;

        return sample / 3.05f;
    }

    private static float GetOrganSample(float phase)
    {
        float fundamental = MathF.Sin(phase * MathF.Tau);
        float second = MathF.Sin((phase * 2f) * MathF.Tau) * 0.45f;
        float third = MathF.Sin((phase * 3f) * MathF.Tau) * 0.26f;
        float fifth = MathF.Sin((phase * 5f) * MathF.Tau) * 0.16f;
        return (fundamental + second + third + fifth) * 0.54f;
    }

    private static float GetMetallicSample(float phase)
    {
        float primary = MathF.Sin(phase * MathF.Tau);
        float partialA = MathF.Sin((phase * 1.4142f) * MathF.Tau) * 0.55f;
        float partialB = MathF.Sin((phase * 2.618f) * MathF.Tau) * 0.30f;
        float ring = MathF.Sin((phase * 4.11f) * MathF.Tau) * 0.18f;
        return Math.Clamp((primary + partialA + partialB + ring) * 0.62f, -1f, 1f);
    }

    private static float GetPinkNoiseSample()
    {
        float whiteA = Random.Shared.NextSingle() * 2f - 1f;
        float whiteB = Random.Shared.NextSingle() * 2f - 1f;
        float whiteC = Random.Shared.NextSingle() * 2f - 1f;
        return Math.Clamp((whiteA * 0.55f) + (whiteB * 0.30f) + (whiteC * 0.15f), -1f, 1f);
    }

    private static float GetSawSample(float phase)
    {
        return (2f * phase) - 1f;
    }

    private static float WrapPhase(float phase)
    {
        phase -= MathF.Floor(phase);
        return phase;
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
