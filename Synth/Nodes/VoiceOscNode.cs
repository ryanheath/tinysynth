using TinySynth.Synth.AudioGraph;
using TinySynth.Synth.Dsp;
using TinySynth.Synth.Modulation;
using TinySynth.Synth.Snapshots;

namespace TinySynth.Synth.Nodes;

internal sealed class VoiceOscNode(string name, SynthVoice voice, VoiceRuntimeContext runtime) : AudioNode(name)
{
    private const int StereoChannelCount = 2;

    private readonly SynthVoice _voice = voice;
    private readonly VoiceRuntimeContext _runtime = runtime;

    protected override void Process(in AudioRenderContext context, IReadOnlyList<AudioNode> inputs, AudioBuffer output)
    {
        _runtime.FilterState.EnsureBuffers(context.FrameCount);

        float[] outputBuffer = output.SampleArray;
        float[] filterCutoffBuffer = _runtime.FilterState.CutoffBuffer;
        float[] filterResonanceBuffer = _runtime.FilterState.ResonanceBuffer;

        if (_voice.EnvelopeStage == EnvelopeStage.Idle || _voice.ActiveMidiNote < 0)
        {
            _runtime.ModulationRuntime.Reset();
            _runtime.FilterState.Reset();
            Array.Clear(filterCutoffBuffer, 0, context.FrameCount);
            Array.Clear(filterResonanceBuffer, 0, context.FrameCount);
            return;
        }

        SynthPatchSnapshot patchSnapshot = context.PatchSnapshot;
        float deltaTime = 1f / _voice.SampleRate;
        int enabledOscillatorCount = GetEnabledOscillatorCount(_voice, patchSnapshot);

        for (int frameIndex = 0; frameIndex < context.FrameCount; frameIndex++)
        {
            if (_voice.EnvelopeStage == EnvelopeStage.Idle || _voice.ActiveMidiNote < 0 || enabledOscillatorCount == 0)
            {
                _runtime.ModulationRuntime.AverageEnvelopeLevel = 0f;
                filterCutoffBuffer[frameIndex] = Math.Clamp(patchSnapshot.FilterCutoffHz, 20f, _voice.SampleRate * 0.45f);
                filterResonanceBuffer[frameIndex] = Math.Clamp(patchSnapshot.FilterResonance, 0f, 1f);
                continue;
            }

            float leftSample = 0f;
            float rightSample = 0f;

            for (int oscillatorIndex = 0; oscillatorIndex < _voice.ActiveOscillatorCount; oscillatorIndex++)
            {
                OscillatorSnapshot oscillatorParameters = patchSnapshot.GetOscillator(oscillatorIndex);
                if (!oscillatorParameters.Enabled)
                {
                    continue;
                }

                SynthVoice.OscillatorState oscillator = _voice.GetOscillatorState(oscillatorIndex);
                UpdateEnvelope(_voice, deltaTime, oscillator, oscillatorParameters);
                UpdateFrequency(_voice, deltaTime, oscillator, oscillatorParameters);
            }

            float envelopeLevel = GetAverageEnvelopeLevel(_voice, patchSnapshot);
            _runtime.ModulationRuntime.AverageEnvelopeLevel = envelopeLevel;
            float keyTrackValue = VoiceRuntimeInspector.GetKeyTrackValue(_voice.ActiveMidiNote);
            ModulationSourceValues rateSourceValues = new(0f, 0f, envelopeLevel, keyTrackValue);
            VoiceModulationState lfoRateModulationState = patchSnapshot.ModulationMatrix.EvaluateVoice(rateSourceValues, oscillatorIndex: -1);
            float lfo1Rate = GetModulatedLfoRate(patchSnapshot.Lfo1.RateHz, lfoRateModulationState.Lfo1Rate);
            float lfo2Rate = GetModulatedLfoRate(patchSnapshot.Lfo2.RateHz, lfoRateModulationState.Lfo2Rate);
            float modulationLfoPhase1 = _runtime.ModulationRuntime.LfoPhase1;
            float modulationLfoPhase2 = _runtime.ModulationRuntime.LfoPhase2;
            float lfo1Value = GetModulationLfoValue(patchSnapshot.Lfo1, deltaTime, ref modulationLfoPhase1, lfo1Rate);
            float lfo2Value = GetModulationLfoValue(patchSnapshot.Lfo2, deltaTime, ref modulationLfoPhase2, lfo2Rate);
            _runtime.ModulationRuntime.LfoPhase1 = modulationLfoPhase1;
            _runtime.ModulationRuntime.LfoPhase2 = modulationLfoPhase2;
            ModulationSourceValues sourceValues = new(lfo1Value, lfo2Value, envelopeLevel, keyTrackValue);
            VoiceModulationState sharedModulationState = patchSnapshot.ModulationMatrix.EvaluateVoice(sourceValues, oscillatorIndex: -1);
            filterCutoffBuffer[frameIndex] = GetModulatedFilterCutoffHz(_voice.SampleRate, patchSnapshot, sharedModulationState);
            filterResonanceBuffer[frameIndex] = Math.Clamp(patchSnapshot.FilterResonance + sharedModulationState.FilterResonance, 0f, 1f);

            for (int oscillatorIndex = 0; oscillatorIndex < _voice.ActiveOscillatorCount; oscillatorIndex++)
            {
                OscillatorSnapshot oscillatorParameters = patchSnapshot.GetOscillator(oscillatorIndex);
                if (!oscillatorParameters.Enabled)
                {
                    continue;
                }

                SynthVoice.OscillatorState oscillator = _voice.GetOscillatorState(oscillatorIndex);

                if (oscillator.EnvelopeLevel <= 0f && _voice.EnvelopeStage == EnvelopeStage.Release)
                {
                    continue;
                }

                VoiceModulationState oscillatorModulationState = patchSnapshot.ModulationMatrix.EvaluateVoice(sourceValues, oscillatorIndex);
                float effectiveFrequency = ApplyVibrato(oscillator.CurrentFrequency, deltaTime, oscillator, oscillatorParameters);
                effectiveFrequency = ApplyPitchModulation(effectiveFrequency, oscillatorModulationState.Pitch);
                float oscillatorSample = GetWaveSample(oscillator, oscillatorParameters, deltaTime, oscillatorModulationState.PulseWidth);
                float gain = Math.Clamp(oscillatorParameters.Gain + oscillatorModulationState.Gain, 0f, 1.25f);
                float oscillatorLevel = oscillatorSample * gain * oscillator.EnvelopeLevel;
                float pan = Math.Clamp(oscillatorParameters.Pan + oscillatorModulationState.Pan, -1f, 1f);
                (float leftGain, float rightGain) = StereoMath.GetPanGains(pan);

                leftSample += oscillatorLevel * leftGain;
                rightSample += oscillatorLevel * rightGain;
                AdvancePhase(_voice, oscillator, effectiveFrequency);
            }

            RefreshEnvelopeStage(_voice, patchSnapshot);

            if (_voice.EnvelopeStage == EnvelopeStage.Idle)
            {
                _runtime.ModulationRuntime.AverageEnvelopeLevel = 0f;
            }

            float normalization = 1f / MathF.Sqrt(enabledOscillatorCount);
            int sampleIndex = frameIndex * StereoChannelCount;
            outputBuffer[sampleIndex] = leftSample * normalization;
            outputBuffer[sampleIndex + 1] = rightSample * normalization;
        }
    }

    private static void UpdateFrequency(SynthVoice voice, float deltaTime, SynthVoice.OscillatorState oscillator, OscillatorSnapshot parameters)
    {
        if (voice.ActiveMidiNote >= 0)
        {
            oscillator.TargetFrequency = PitchMath.ApplyDetune(MidiUtilities.MidiToFrequency(voice.ActiveMidiNote), parameters.DetuneCents);
        }

        if (parameters.GlideSeconds <= 0.001f)
        {
            oscillator.CurrentFrequency = oscillator.TargetFrequency;
            return;
        }

        float glideFactor = MathF.Min(deltaTime / parameters.GlideSeconds, 1f);
        oscillator.CurrentFrequency += (oscillator.TargetFrequency - oscillator.CurrentFrequency) * glideFactor;
    }

    private static void UpdateEnvelope(SynthVoice voice, float deltaTime, SynthVoice.OscillatorState oscillator, OscillatorSnapshot parameters)
    {
        switch (voice.EnvelopeStage)
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

    private static void RefreshEnvelopeStage(SynthVoice voice, SynthPatchSnapshot patchSnapshot)
    {
        if (!HasEnabledOscillator(voice, patchSnapshot))
        {
            SetVoiceStage(voice, activeMidiNote: -1, EnvelopeStage.Idle);
            return;
        }

        if (voice.EnvelopeStage == EnvelopeStage.Release)
        {
            if (VoiceRuntimeInspector.AreAllOscillatorsAtOrBelow(voice, 0f))
            {
                SetVoiceStage(voice, activeMidiNote: -1, EnvelopeStage.Idle);
            }

            return;
        }

        if (voice.EnvelopeStage == EnvelopeStage.Attack)
        {
            if (AreAllEnabledOscillatorsAtOrAbove(voice, patchSnapshot, 1f))
            {
                SetVoiceStage(voice, voice.ActiveMidiNote, EnvelopeStage.Decay);
            }

            return;
        }

        if (voice.EnvelopeStage != EnvelopeStage.Decay)
        {
            return;
        }

        bool hasSustainOscillator = false;

        for (int i = 0; i < voice.ActiveOscillatorCount; i++)
        {
            OscillatorSnapshot oscillatorParameters = patchSnapshot.GetOscillator(i);

            if (!oscillatorParameters.Enabled)
            {
                continue;
            }

            if (oscillatorParameters.EnvelopeMode == EnvelopeMode.OneShot)
            {
                if (voice.GetOscillatorState(i).EnvelopeLevel > 0f)
                {
                    return;
                }

                continue;
            }

            hasSustainOscillator = true;

            if (voice.GetOscillatorState(i).EnvelopeLevel > oscillatorParameters.SustainLevel)
            {
                return;
            }
        }

        if (!hasSustainOscillator)
        {
            SetVoiceStage(voice, activeMidiNote: -1, EnvelopeStage.Idle);
            return;
        }

        SetVoiceStage(voice, voice.ActiveMidiNote, EnvelopeStage.Sustain);
    }

    private static void SetVoiceStage(SynthVoice voice, int activeMidiNote, EnvelopeStage envelopeStage)
    {
        voice.SetEnvelopeStageState(activeMidiNote, envelopeStage);
    }

    private static float ApplyVibrato(float baseFrequency, float deltaTime, SynthVoice.OscillatorState oscillator, OscillatorSnapshot parameters)
    {
        if (parameters.VibratoDepthCents <= 0f || parameters.VibratoRateHz <= 0f)
        {
            return baseFrequency;
        }

        oscillator.VibratoPhase += parameters.VibratoRateHz * deltaTime;
        oscillator.VibratoPhase -= MathF.Floor(oscillator.VibratoPhase);

        float vibratoCents = MathF.Sin(oscillator.VibratoPhase * MathF.Tau) * parameters.VibratoDepthCents;
        return PitchMath.ApplyDetune(baseFrequency, vibratoCents);
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

        return ModulationLfoEvaluator.Evaluate(parameters, phase);
    }

    private static float ApplyPitchModulation(float baseFrequency, float pitchModulation)
    {
        float pitchCents = pitchModulation * 120f;
        return PitchMath.ApplyDetune(baseFrequency, pitchCents);
    }

    private static int GetEnabledOscillatorCount(SynthVoice voice, SynthPatchSnapshot patchSnapshot)
    {
        int count = 0;

        for (int i = 0; i < voice.ActiveOscillatorCount; i++)
        {
            if (patchSnapshot.GetOscillator(i).Enabled)
            {
                count++;
            }
        }

        return count;
    }

    private static float GetAverageEnvelopeLevel(SynthVoice voice, SynthPatchSnapshot patchSnapshot)
    {
        float totalEnvelope = 0f;
        int enabledOscillatorCount = 0;

        for (int i = 0; i < voice.ActiveOscillatorCount; i++)
        {
            if (!patchSnapshot.GetOscillator(i).Enabled)
            {
                continue;
            }

            totalEnvelope += voice.GetOscillatorState(i).EnvelopeLevel;
            enabledOscillatorCount++;
        }

        return enabledOscillatorCount == 0 ? 0f : totalEnvelope / enabledOscillatorCount;
    }

    private static float GetModulatedFilterCutoffHz(int sampleRate, SynthPatchSnapshot patchSnapshot, VoiceModulationState modulationState)
    {
        float baseCutoffHz = Math.Clamp(patchSnapshot.FilterCutoffHz, 20f, sampleRate * 0.45f);
        float modulatedCutoffHz = baseCutoffHz * MathF.Pow(2f, modulationState.FilterCutoff);
        return Math.Clamp(modulatedCutoffHz, 20f, sampleRate * 0.45f);
    }

    private static bool HasEnabledOscillator(SynthVoice voice, SynthPatchSnapshot patchSnapshot)
    {
        for (int i = 0; i < voice.ActiveOscillatorCount; i++)
        {
            if (patchSnapshot.GetOscillator(i).Enabled)
            {
                return true;
            }
        }

        return false;
    }

    private static bool AreAllEnabledOscillatorsAtOrAbove(SynthVoice voice, SynthPatchSnapshot patchSnapshot, float value)
    {
        bool hasEnabledOscillator = false;

        for (int i = 0; i < voice.ActiveOscillatorCount; i++)
        {
            if (!patchSnapshot.GetOscillator(i).Enabled)
            {
                continue;
            }

            hasEnabledOscillator = true;

            if (voice.GetOscillatorState(i).EnvelopeLevel < value)
            {
                return false;
            }
        }

        return hasEnabledOscillator;
    }

    private static float GetWaveSample(SynthVoice.OscillatorState oscillator, OscillatorSnapshot parameters, float deltaTime, float pulseWidthModulation)
    {
        float phase = oscillator.Phase;
        return parameters.Waveform switch
        {
            Waveform.Sine => MathF.Sin(phase * MathF.Tau),
            Waveform.Square => phase < 0.5f ? 1f : -1f,
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

    private static float GetPulseSample(SynthVoice.OscillatorState oscillator, OscillatorSnapshot parameters, float deltaTime, float pulseWidthModulation)
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

    private static void AdvancePhase(SynthVoice voice, SynthVoice.OscillatorState oscillator, float frequency)
    {
        oscillator.Phase += frequency / voice.SampleRate;
        oscillator.Phase -= MathF.Floor(oscillator.Phase);
    }

}
