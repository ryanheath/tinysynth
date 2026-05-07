namespace TinySynth.Synth;

internal sealed class SynthEngine
{
    private readonly VoiceSlot[] _voiceSlots;
    private readonly HashSet<int> _activeNotes = [];
    private bool _holdPedalEnabled;
    private long _voiceStartCounter;

    public SynthEngine(int sampleRate, float masterGain, int defaultMidiNote, int voiceCount)
    {
        voiceCount = Math.Max(1, voiceCount);
        _voiceSlots = new VoiceSlot[voiceCount];

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
            float sample = MathF.Tanh(audioBuffer[i] * mixScale);
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
}
