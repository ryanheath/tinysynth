namespace TinySynth.Synth;

internal sealed class SynthEngine
{
    private readonly int _sampleRate;
    private readonly VoiceSlot[] _voiceSlots;
    private readonly HashSet<int> _activeNotes = [];
    private readonly float[] _chorusBuffer;
    private readonly ReverbDelayLine[] _reverbLines;
    private bool _holdPedalEnabled;
    private int _chorusWriteIndex;
    private long _voiceStartCounter;
    private float _chorusPhaseA;
    private float _chorusPhaseB;

    public SynthEngine(int sampleRate, float masterGain, int defaultMidiNote, int voiceCount)
    {
        _sampleRate = sampleRate;
        voiceCount = Math.Max(1, voiceCount);
        _voiceSlots = new VoiceSlot[voiceCount];
        _chorusBuffer = new float[Math.Max(2048, sampleRate / 2)];
        _reverbLines =
        [
            new ReverbDelayLine(sampleRate, 0.097f),
            new ReverbDelayLine(sampleRate, 0.131f),
            new ReverbDelayLine(sampleRate, 0.173f),
            new ReverbDelayLine(sampleRate, 0.211f)
        ];

        for (int i = 0; i < voiceCount; i++)
        {
            _voiceSlots[i] = new VoiceSlot(new SynthVoice(sampleRate, masterGain, defaultMidiNote));
        }

        RefreshVoiceState();
    }

    public IReadOnlySet<int> ActiveNotes => _activeNotes;

    public int ActiveVoiceCount { get; private set; }

    public int DisplayMidiNote => GetDisplaySlot()?.Voice.ActiveMidiNote ?? -1;

    public float DisplayFrequency => GetDisplaySlot()?.Voice.CurrentFrequency ?? 0f;

    public EnvelopeStage DisplayEnvelopeStage => GetDisplaySlot()?.Voice.EnvelopeStage ?? EnvelopeStage.Idle;

    public void SetMasterGain(float masterGain)
    {
        foreach (VoiceSlot slot in _voiceSlots)
        {
            slot.Voice.SetMasterGain(masterGain);
        }
    }

    public void SetHoldPedal(bool enabled)
    {
        if (_holdPedalEnabled == enabled)
        {
            return;
        }

        _holdPedalEnabled = enabled;

        if (!_holdPedalEnabled)
        {
            ReleaseUnheldVoices();
        }

        RefreshVoiceState();
    }

    public void NoteOn(int midiNote, SynthParameters parameters)
    {
        VoiceSlot slot = FindSlotForNoteOn(midiNote);
        bool forceRestart = slot.Voice.ActiveMidiNote >= 0 && slot.Voice.ActiveMidiNote != midiNote;

        slot.IsHeld = true;
        slot.LastStartOrder = ++_voiceStartCounter;
        slot.Voice.StartNote(midiNote, parameters, forceRestart);

        RefreshVoiceState();
    }

    public void NoteOff(int midiNote)
    {
        foreach (VoiceSlot slot in _voiceSlots)
        {
            if (slot.Voice.ActiveMidiNote != midiNote || slot.Voice.IsIdle)
            {
                continue;
            }

            slot.IsHeld = false;

            if (!_holdPedalEnabled)
            {
                slot.Voice.ReleaseNote();
            }
        }

        RefreshVoiceState();
    }

    public void FillBuffer(float[] audioBuffer, float[] scopeBuffer, ref int scopeWriteIndex, SynthParameters parameters)
    {
        Array.Clear(audioBuffer);

        int mixedVoiceCount = 0;

        foreach (VoiceSlot slot in _voiceSlots)
        {
            if (slot.Voice.IsIdle)
            {
                continue;
            }

            mixedVoiceCount++;
            slot.Voice.AddToBuffer(audioBuffer, parameters);
        }

        float mixScale = mixedVoiceCount > 1
            ? 1f / MathF.Sqrt(mixedVoiceCount)
            : 1f;

        for (int i = 0; i < audioBuffer.Length; i++)
        {
            float sample = audioBuffer[i] * mixScale;
            sample = ProcessEffects(sample, parameters);
            sample = MathF.Tanh(sample);
            audioBuffer[i] = sample;
            scopeBuffer[scopeWriteIndex] = sample;
            scopeWriteIndex = (scopeWriteIndex + 1) % scopeBuffer.Length;
        }

        RefreshVoiceState();
    }

    private VoiceSlot FindSlotForNoteOn(int midiNote)
    {
        return FindSlotPlayingNote(midiNote)
            ?? FindIdleSlot()
            ?? FindReleasingSlot()
            ?? FindOldestActiveSlot();
    }

    private VoiceSlot? FindSlotPlayingNote(int midiNote)
    {
        VoiceSlot? best = null;

        foreach (VoiceSlot slot in _voiceSlots)
        {
            if (slot.Voice.ActiveMidiNote != midiNote || slot.Voice.IsIdle)
            {
                continue;
            }

            if (best is null || slot.LastStartOrder > best.LastStartOrder)
            {
                best = slot;
            }
        }

        return best;
    }

    private VoiceSlot? FindIdleSlot()
    {
        foreach (VoiceSlot slot in _voiceSlots)
        {
            if (slot.Voice.IsIdle || slot.Voice.ActiveMidiNote < 0)
            {
                return slot;
            }
        }

        return null;
    }

    private VoiceSlot? FindReleasingSlot()
    {
        VoiceSlot? candidate = null;

        foreach (VoiceSlot slot in _voiceSlots)
        {
            if (slot.Voice.EnvelopeStage != EnvelopeStage.Release)
            {
                continue;
            }

            if (candidate is null || slot.LastStartOrder < candidate.LastStartOrder)
            {
                candidate = slot;
            }
        }

        return candidate;
    }

    private VoiceSlot FindOldestActiveSlot()
    {
        VoiceSlot oldest = _voiceSlots[0];

        foreach (VoiceSlot slot in _voiceSlots)
        {
            if (slot.LastStartOrder < oldest.LastStartOrder)
            {
                oldest = slot;
            }
        }

        return oldest;
    }

    private VoiceSlot? GetDisplaySlot()
    {
        VoiceSlot? displaySlot = null;

        foreach (VoiceSlot slot in _voiceSlots)
        {
            if (slot.Voice.IsIdle || slot.Voice.ActiveMidiNote < 0)
            {
                continue;
            }

            if (displaySlot is null || slot.LastStartOrder > displaySlot.LastStartOrder)
            {
                displaySlot = slot;
            }
        }

        return displaySlot;
    }

    private void ReleaseUnheldVoices()
    {
        foreach (VoiceSlot slot in _voiceSlots)
        {
            if (!slot.IsHeld && !slot.Voice.IsIdle)
            {
                slot.Voice.ReleaseNote();
            }
        }
    }

    private void RefreshVoiceState()
    {
        _activeNotes.Clear();
        ActiveVoiceCount = 0;

        foreach (VoiceSlot slot in _voiceSlots)
        {
            if (slot.Voice.IsIdle || slot.Voice.ActiveMidiNote < 0)
            {
                slot.IsHeld = false;
                continue;
            }

            _activeNotes.Add(slot.Voice.ActiveMidiNote);
            ActiveVoiceCount++;
        }
    }

    private float ProcessEffects(float inputSample, SynthParameters parameters)
    {
        float sample = ProcessChorus(inputSample, parameters);
        sample = ProcessReverb(sample, parameters);
        return sample;
    }

    private float ProcessChorus(float inputSample, SynthParameters parameters)
    {
        if (parameters.ChorusType == ChorusType.Off || parameters.ChorusMix <= 0.0001f)
        {
            return inputSample;
        }

        (float baseDelayMs, float depthMs, float rateScale, float feedback) = parameters.ChorusType switch
        {
            ChorusType.Light => (11f, 4f, 1.0f, 0.08f),
            ChorusType.Ensemble => (16f, 7f, 1.15f, 0.12f),
            ChorusType.Wide => (21f, 10f, 0.85f, 0.18f),
            _ => (0f, 0f, 1f, 0f)
        };

        float rateHz = Math.Clamp(parameters.ChorusRateHz * rateScale, 0.05f, 4f);
        float depthScale = Math.Clamp(parameters.ChorusDepth, 0.05f, 1f);
        float mix = Math.Clamp(parameters.ChorusMix, 0f, 1f);

        _chorusPhaseA = AdvancePhase(_chorusPhaseA, rateHz / _sampleRate);
        _chorusPhaseB = AdvancePhase(_chorusPhaseB, (rateHz * 1.31f) / _sampleRate);

        float delaySamplesA = ((baseDelayMs + (MathF.Sin(_chorusPhaseA * MathF.Tau) * depthMs * depthScale)) * _sampleRate) / 1000f;
        float delaySamplesB = ((baseDelayMs + (MathF.Sin((_chorusPhaseB * MathF.Tau) + 1.7f) * depthMs * depthScale)) * _sampleRate) / 1000f;
        delaySamplesA = Math.Clamp(delaySamplesA, 1f, _chorusBuffer.Length - 2f);
        delaySamplesB = Math.Clamp(delaySamplesB, 1f, _chorusBuffer.Length - 2f);

        float tapA = ReadDelay(_chorusBuffer, _chorusWriteIndex, delaySamplesA);
        float tapB = ReadDelay(_chorusBuffer, _chorusWriteIndex, delaySamplesB);
        float wetSample = (tapA + tapB) * 0.5f;

        _chorusBuffer[_chorusWriteIndex] = inputSample + (wetSample * feedback);
        _chorusWriteIndex = (_chorusWriteIndex + 1) % _chorusBuffer.Length;

        return (inputSample * (1f - (mix * 0.45f))) + (wetSample * mix);
    }

    private float ProcessReverb(float inputSample, SynthParameters parameters)
    {
        if (parameters.ReverbType == ReverbType.Off || parameters.ReverbMix <= 0.0001f)
        {
            return inputSample;
        }

        (float sizeScale, float feedback, float brightness) = parameters.ReverbType switch
        {
            ReverbType.Room => (0.72f, 0.58f, 0.48f),
            ReverbType.Hall => (0.92f, 0.72f, 0.38f),
            ReverbType.Shimmer => (1.00f, 0.78f, 0.62f),
            _ => (0f, 0f, 0f)
        };

        float mix = Math.Clamp(parameters.ReverbMix, 0f, 1f);
        float size = Math.Clamp(parameters.ReverbSize, 0.10f, 1f);
        float damping = Math.Clamp(parameters.ReverbDamping, 0f, 1f);
        float toneResponse = Math.Clamp((1f - (damping * 0.85f)) * (0.55f + (brightness * 0.45f)), 0.05f, 0.95f);
        float effectiveFeedback = Math.Clamp(feedback + ((size - 0.5f) * 0.18f), 0.25f, 0.88f);
        float wetSum = 0f;

        for (int i = 0; i < _reverbLines.Length; i++)
        {
            ReverbDelayLine line = _reverbLines[i];
            float delaySamples = line.MaxDelaySamples * Math.Clamp((0.45f + (size * 0.55f)) * sizeScale, 0.20f, 1f);
            float delayed = ReadDelay(line.Buffer, line.WriteIndex, delaySamples);
            line.FilterState += (delayed - line.FilterState) * toneResponse;
            float filtered = line.FilterState;
            float polarity = (i & 1) == 0 ? 1f : -1f;

            line.Buffer[line.WriteIndex] = inputSample + (filtered * effectiveFeedback);
            line.WriteIndex = (line.WriteIndex + 1) % line.Buffer.Length;
            wetSum += filtered * polarity;
        }

        float wetSample = wetSum * 0.35f;
        return (inputSample * (1f - (mix * 0.55f))) + (wetSample * mix);
    }

    private static float AdvancePhase(float phase, float increment)
    {
        phase += increment;
        phase -= MathF.Floor(phase);
        return phase;
    }

    private static float ReadDelay(float[] buffer, int writeIndex, float delaySamples)
    {
        float readPosition = writeIndex - delaySamples;

        while (readPosition < 0f)
        {
            readPosition += buffer.Length;
        }

        int sampleIndexA = (int)readPosition;
        int sampleIndexB = (sampleIndexA + 1) % buffer.Length;
        float fraction = readPosition - sampleIndexA;
        return buffer[sampleIndexA] + ((buffer[sampleIndexB] - buffer[sampleIndexA]) * fraction);
    }

    private sealed class VoiceSlot
    {
        public VoiceSlot(SynthVoice voice)
        {
            Voice = voice;
        }

        public SynthVoice Voice { get; }

        public bool IsHeld { get; set; }

        public long LastStartOrder { get; set; }
    }

    private sealed class ReverbDelayLine
    {
        public ReverbDelayLine(int sampleRate, float maxDelaySeconds)
        {
            Buffer = new float[Math.Max(64, (int)MathF.Ceiling(sampleRate * maxDelaySeconds))];
        }

        public float[] Buffer { get; }

        public float FilterState { get; set; }

        public int MaxDelaySamples => Buffer.Length - 1;

        public int WriteIndex { get; set; }
    }
}
