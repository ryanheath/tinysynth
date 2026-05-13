namespace TinySynth.Synth;

internal sealed class VoicePool
{
    private readonly VoiceSlot[] _voiceSlots;
    private long _voiceStartCounter;

    public VoicePool(int sampleRate, float masterGain, int defaultMidiNote, int voiceCount)
    {
        voiceCount = Math.Max(1, voiceCount);
        _voiceSlots = new VoiceSlot[voiceCount];

        for (int i = 0; i < voiceCount; i++)
        {
            SynthVoice voice = new(sampleRate, masterGain, defaultMidiNote);
            _voiceSlots[i] = new VoiceSlot(voice);
        }
    }

    public IReadOnlyList<VoiceSlot> Slots => _voiceSlots;

    public void SetMasterGain(float masterGain)
    {
        foreach (VoiceSlot slot in _voiceSlots)
        {
            slot.Voice.SetMasterGain(masterGain);
        }
    }

    public void StartNote(int midiNote, SynthParameters parameters)
    {
        VoiceSlot slot = FindSlotForNoteOn(midiNote);
        bool forceRestart = slot.Voice.ActiveMidiNote >= 0 && slot.Voice.ActiveMidiNote != midiNote;

        slot.IsHeld = true;
        slot.LastStartOrder = ++_voiceStartCounter;
        slot.Voice.StartNote(midiNote, parameters, forceRestart);
    }

    public void ReleaseNote(int midiNote, bool holdPedalEnabled)
    {
        foreach (VoiceSlot slot in _voiceSlots)
        {
            if (slot.Voice.ActiveMidiNote != midiNote || slot.Voice.IsIdle)
            {
                continue;
            }

            slot.IsHeld = false;

            if (!holdPedalEnabled)
            {
                slot.Voice.ReleaseNote();
            }
        }
    }

    public void ClearHeldStateForIdleVoices()
    {
        foreach (VoiceSlot slot in _voiceSlots)
        {
            if (slot.Voice.IsIdle || slot.Voice.ActiveMidiNote < 0)
            {
                slot.IsHeld = false;
            }
        }
    }

    public void ReleaseUnheldVoices()
    {
        foreach (VoiceSlot slot in _voiceSlots)
        {
            if (!slot.IsHeld && !slot.Voice.IsIdle)
            {
                slot.Voice.ReleaseNote();
            }
        }
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
}
