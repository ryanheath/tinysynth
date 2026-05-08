namespace TinySynth.Synth;

internal sealed class SynthEngine
{
    private const int StereoChannelCount = 2;

    private readonly int _sampleRate;
    private readonly VoiceSlot[] _voiceSlots;
    private readonly HashSet<int> _activeNotes = [];
    private readonly float[][] _chorusBuffers;
    private readonly float[][] _delayBuffers;
    private readonly ReverbDelayLine[] _reverbLines;
    private bool _holdPedalEnabled;
    private int _chorusWriteIndex;
    private int _delayWriteIndex;
    private long _voiceStartCounter;
    private float _chorusPhaseA;
    private float _chorusPhaseB;
    private readonly float[] _delayFilterState = new float[StereoChannelCount];

    public SynthEngine(int sampleRate, float masterGain, int defaultMidiNote, int voiceCount)
    {
        _sampleRate = sampleRate;
        voiceCount = Math.Max(1, voiceCount);
        _voiceSlots = new VoiceSlot[voiceCount];
        int chorusBufferLength = Math.Max(2048, sampleRate / 2);
        int delayBufferLength = Math.Max(4096, sampleRate);
        _chorusBuffers =
        [
            new float[chorusBufferLength],
            new float[chorusBufferLength]
        ];
        _delayBuffers =
        [
            new float[delayBufferLength],
            new float[delayBufferLength]
        ];
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

        for (int i = 0; i < audioBuffer.Length; i += StereoChannelCount)
        {
            float leftSample = audioBuffer[i] * mixScale;
            float rightSample = audioBuffer[i + 1] * mixScale;
            (leftSample, rightSample) = ProcessEffects(leftSample, rightSample, parameters);
            leftSample = MathF.Tanh(leftSample);
            rightSample = MathF.Tanh(rightSample);
            audioBuffer[i] = leftSample;
            audioBuffer[i + 1] = rightSample;
            scopeBuffer[scopeWriteIndex] = (leftSample + rightSample) * 0.5f;
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

    private (float Left, float Right) ProcessEffects(float leftInput, float rightInput, SynthParameters parameters)
    {
        (float left, float right) = ProcessChorus(leftInput, rightInput, parameters);
        (left, right) = ProcessReverb(left, right, parameters);
        (left, right) = ProcessDelay(left, right, parameters);
        return (left, right);
    }

    private (float Left, float Right) ProcessChorus(float inputLeft, float inputRight, SynthParameters parameters)
    {
        if (parameters.ChorusType == ChorusType.Off || parameters.ChorusMix <= 0.0001f)
        {
            return (inputLeft, inputRight);
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
        float tremoloDepth = Math.Clamp(parameters.ChorusTremoloDepth, 0f, 1f);

        _chorusPhaseA = AdvancePhase(_chorusPhaseA, rateHz / _sampleRate);
        _chorusPhaseB = AdvancePhase(_chorusPhaseB, (rateHz * 1.31f) / _sampleRate);

        float delaySamplesA = ((baseDelayMs + (MathF.Sin(_chorusPhaseA * MathF.Tau) * depthMs * depthScale)) * _sampleRate) / 1000f;
        float delaySamplesB = ((baseDelayMs + (MathF.Sin((_chorusPhaseB * MathF.Tau) + 1.7f) * depthMs * depthScale)) * _sampleRate) / 1000f;
        delaySamplesA = Math.Clamp(delaySamplesA, 1f, _chorusBuffers[0].Length - 2f);
        delaySamplesB = Math.Clamp(delaySamplesB, 1f, _chorusBuffers[0].Length - 2f);

        float wetLeftA = ReadDelay(_chorusBuffers[0], _chorusWriteIndex, delaySamplesA);
        float wetLeftB = ReadDelay(_chorusBuffers[0], _chorusWriteIndex, delaySamplesB);
        float wetRightA = ReadDelay(_chorusBuffers[1], _chorusWriteIndex, delaySamplesB);
        float wetRightB = ReadDelay(_chorusBuffers[1], _chorusWriteIndex, delaySamplesA);
        float wetLeft = (wetLeftA * 0.7f) + (wetRightA * 0.3f);
        float wetRight = (wetRightB * 0.7f) + (wetLeftB * 0.3f);
        float tremolo = 1f - (((MathF.Sin((_chorusPhaseA * MathF.Tau) + 0.9f) + 1f) * 0.5f) * tremoloDepth);
        wetLeft *= tremolo;
        wetRight *= 1f - (((MathF.Sin((_chorusPhaseB * MathF.Tau) + 2.1f) + 1f) * 0.5f) * tremoloDepth);

        _chorusBuffers[0][_chorusWriteIndex] = inputLeft + (wetLeft * feedback);
        _chorusBuffers[1][_chorusWriteIndex] = inputRight + (wetRight * feedback);
        _chorusWriteIndex = (_chorusWriteIndex + 1) % _chorusBuffers[0].Length;

        return (
            (inputLeft * (1f - (mix * 0.45f))) + (wetLeft * mix),
            (inputRight * (1f - (mix * 0.45f))) + (wetRight * mix));
    }

    private (float Left, float Right) ProcessReverb(float inputLeft, float inputRight, SynthParameters parameters)
    {
        if (parameters.ReverbType == ReverbType.Off || parameters.ReverbMix <= 0.0001f)
        {
            return (inputLeft, inputRight);
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
        float crossFeed = 0.10f;
        float wetLeftSum = 0f;
        float wetRightSum = 0f;

        for (int i = 0; i < _reverbLines.Length; i++)
        {
            ReverbDelayLine line = _reverbLines[i];
            float delaySamples = line.MaxDelaySamples * Math.Clamp((0.45f + (size * 0.55f)) * sizeScale, 0.20f, 1f);
            float delayedLeft = ReadDelay(line.LeftBuffer, line.WriteIndex, delaySamples);
            float delayedRight = ReadDelay(line.RightBuffer, line.WriteIndex, delaySamples);
            line.FilterStateLeft += (delayedLeft - line.FilterStateLeft) * toneResponse;
            line.FilterStateRight += (delayedRight - line.FilterStateRight) * toneResponse;
            float filteredLeft = line.FilterStateLeft;
            float filteredRight = line.FilterStateRight;
            float polarity = (i & 1) == 0 ? 1f : -1f;

            line.LeftBuffer[line.WriteIndex] = inputLeft + (filteredLeft * effectiveFeedback) + (filteredRight * crossFeed);
            line.RightBuffer[line.WriteIndex] = inputRight + (filteredRight * effectiveFeedback) + (filteredLeft * crossFeed);
            line.WriteIndex = (line.WriteIndex + 1) % line.Buffer.Length;
            wetLeftSum += filteredLeft * polarity;
            wetRightSum += filteredRight * -polarity;
        }

        float wetLeft = wetLeftSum * 0.35f;
        float wetRight = wetRightSum * 0.35f;
        return (
            (inputLeft * (1f - (mix * 0.55f))) + (wetLeft * mix),
            (inputRight * (1f - (mix * 0.55f))) + (wetRight * mix));
    }

    private (float Left, float Right) ProcessDelay(float inputLeft, float inputRight, SynthParameters parameters)
    {
        if (parameters.DelayType == DelayType.Off || parameters.DelayMix <= 0.0001f)
        {
            return (inputLeft, inputRight);
        }

        (float timeScale, float feedbackScale, float toneResponse, float modulationDepth) = parameters.DelayType switch
        {
            DelayType.Slap => (0.45f, 0.55f, 0.72f, 0f),
            DelayType.PingPong => (0.85f, 0.70f, 0.58f, 0.0015f),
            DelayType.Tape => (1.00f, 0.78f, 0.42f, 0.0035f),
            _ => (0f, 0f, 1f, 0f)
        };

        float mix = Math.Clamp(parameters.DelayMix, 0f, 1f);
        float delaySeconds = Math.Clamp(parameters.DelayTimeSeconds * timeScale, 0.04f, 0.95f);
        float feedback = Math.Clamp(parameters.DelayFeedback * feedbackScale, 0f, 0.88f);
        float modulation = modulationDepth <= 0f
            ? 0f
            : MathF.Sin(_chorusPhaseA * MathF.Tau) * (_sampleRate * modulationDepth);
        float delaySamples = Math.Clamp((delaySeconds * _sampleRate) + modulation, 1f, _delayBuffers[0].Length - 2f);
        float delayedLeft = ReadDelay(_delayBuffers[0], _delayWriteIndex, delaySamples);
        float delayedRight = ReadDelay(_delayBuffers[1], _delayWriteIndex, delaySamples);

        _delayFilterState[0] += (delayedLeft - _delayFilterState[0]) * toneResponse;
        _delayFilterState[1] += (delayedRight - _delayFilterState[1]) * toneResponse;
        float filteredLeft = _delayFilterState[0];
        float filteredRight = _delayFilterState[1];

        if (parameters.DelayType == DelayType.PingPong)
        {
            _delayBuffers[0][_delayWriteIndex] = inputLeft + (filteredRight * feedback);
            _delayBuffers[1][_delayWriteIndex] = inputRight + (filteredLeft * feedback);
        }
        else
        {
            _delayBuffers[0][_delayWriteIndex] = inputLeft + (filteredLeft * feedback);
            _delayBuffers[1][_delayWriteIndex] = inputRight + (filteredRight * feedback);
        }

        _delayWriteIndex = (_delayWriteIndex + 1) % _delayBuffers[0].Length;

        return (
            (inputLeft * (1f - (mix * 0.45f))) + (filteredLeft * mix),
            (inputRight * (1f - (mix * 0.45f))) + (filteredRight * mix));
    }

    private static float AdvancePhase(float phase, float increment)
    {
        phase += increment;
        phase -= MathF.Floor(phase);
        return phase;
    }

    private static float ReadDelay(float[] buffer, int writeIndex, float delaySamples)
    {
        double readPosition = writeIndex - delaySamples;
        readPosition %= buffer.Length;

        if (readPosition < 0d)
        {
            readPosition += buffer.Length;
        }

        int sampleIndexA = (int)readPosition;

        if (sampleIndexA >= buffer.Length)
        {
            sampleIndexA = 0;
        }

        int sampleIndexB = (sampleIndexA + 1) % buffer.Length;
        float fraction = (float)(readPosition - sampleIndexA);
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
            int bufferLength = Math.Max(64, (int)MathF.Ceiling(sampleRate * maxDelaySeconds));
            LeftBuffer = new float[bufferLength];
            RightBuffer = new float[bufferLength];
        }

        public float[] LeftBuffer { get; }

        public float[] RightBuffer { get; }

        public float[] Buffer => LeftBuffer;

        public float FilterStateLeft { get; set; }

        public float FilterStateRight { get; set; }

        public int MaxDelaySamples => Buffer.Length - 1;

        public int WriteIndex { get; set; }
    }
}
