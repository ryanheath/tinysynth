using TinySynth.Synth.AudioGraph;
using TinySynth.Synth.Modulation;
using TinySynth.Synth.Nodes;
using TinySynth.Synth.Snapshots;

namespace TinySynth.Synth;

internal sealed class SynthEngine
{
    private const int StereoChannelCount = 2;

    private readonly int _sampleRate;
    private readonly VoiceSlot[] _voiceSlots;
    private readonly SynthVoice[] _voices;
    private readonly HashSet<int> _activeNotes = [];
    private bool _holdPedalEnabled;
    private long _voiceStartCounter;
    private readonly AudioGraphScheduler _audioGraphScheduler;

    private float _lastEnvelopeLevel;

    private int _lastKeyTrackMidiNote = -1;
    private int _fallbackBlockId;

    public SynthEngine(int sampleRate, float masterGain, int defaultMidiNote, int voiceCount)
    {
        _sampleRate = sampleRate;
        voiceCount = Math.Max(1, voiceCount);
        _voiceSlots = new VoiceSlot[voiceCount];
        _voices = new SynthVoice[voiceCount];

        for (int i = 0; i < voiceCount; i++)
        {
            SynthVoice voice = new(sampleRate, masterGain, defaultMidiNote);
            _voices[i] = voice;
            _voiceSlots[i] = new VoiceSlot(voice);
        }

        _audioGraphScheduler = CreateAudioGraphScheduler();
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
        RenderBlock(audioBuffer, scopeBuffer, ref scopeWriteIndex, ++_fallbackBlockId, parameters);
    }

    public void RenderBlock(float[] audioBuffer, float[] scopeBuffer, ref int scopeWriteIndex, int blockId, SynthParameters parameters)
    {
        SynthPatchSnapshot patchSnapshot = SynthPatchSnapshot.Create(parameters);
        UpdateModulationState();
        GlobalModulationState globalModulationState = GetGlobalModulationState(patchSnapshot);
        AudioRenderContext context = new(blockId, audioBuffer.Length / StereoChannelCount, _sampleRate, patchSnapshot, globalModulationState);
        AudioBuffer output = _audioGraphScheduler.Execute(context);
        output.CopyTo(audioBuffer);

        for (int i = 0; i < audioBuffer.Length; i += StereoChannelCount)
        {
            scopeBuffer[scopeWriteIndex] = (audioBuffer[i] + audioBuffer[i + 1]) * 0.5f;
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

    private AudioGraphScheduler CreateAudioGraphScheduler()
    {
        RenderSourceNode voiceBusNode = new("VoiceBus", _voices);
        ChorusNode chorusNode = new("Chorus", _sampleRate, voiceBusNode);
        DelayNode delayNode = new("Delay", _sampleRate, voiceBusNode);
        ReverbNode reverbNode = new("Reverb", _sampleRate, voiceBusNode);
        MainMixerNode mainMixerNode = new("MainMixer", voiceBusNode, chorusNode, delayNode, reverbNode);
        OutputNode outputNode = new("Output", 1f, mainMixerNode);
        return new AudioGraphScheduler(new AudioGraph.AudioGraph(outputNode));
    }

    private void UpdateModulationState()
    {
        _lastEnvelopeLevel = 0f;
        _lastKeyTrackMidiNote = -1;
        int voiceCount = 0;

        foreach (VoiceSlot slot in _voiceSlots)
        {
            if (slot.Voice.IsIdle || slot.Voice.ActiveMidiNote < 0)
            {
                continue;
            }

            _lastEnvelopeLevel += slot.Voice.ModulationEnvelopeLevel;
            _lastKeyTrackMidiNote = Math.Max(_lastKeyTrackMidiNote, slot.Voice.ActiveMidiNote);
            voiceCount++;
        }

        if (voiceCount > 0)
        {
            _lastEnvelopeLevel /= voiceCount;
        }
    }

    private GlobalModulationState GetGlobalModulationState(SynthPatchSnapshot patchSnapshot)
    {
        float lfo1Value = GetLfoValue(patchSnapshot.Lfo1, 0f);
        float lfo2Value = GetLfoValue(patchSnapshot.Lfo2, 0f);
        float keyTrackValue = _lastKeyTrackMidiNote < 0
            ? 0f
            : Math.Clamp((_lastKeyTrackMidiNote - 60f) / 24f, -1f, 1f);

        ModulationSourceValues sourceValues = new(lfo1Value, lfo2Value, _lastEnvelopeLevel, keyTrackValue);
        return patchSnapshot.ModulationMatrix.EvaluateGlobal(sourceValues);
    }

    private static float GetLfoValue(ModulationLfoSnapshot parameters, float phase)
    {
        float value = parameters.Shape switch
        {
            ModulationLfoShape.Sine => MathF.Sin(phase * MathF.Tau),
            ModulationLfoShape.Triangle => 1f - (4f * MathF.Abs(phase - 0.5f)),
            ModulationLfoShape.Saw => (2f * phase) - 1f,
            ModulationLfoShape.Square => phase < 0.5f ? 1f : -1f,
            _ => 0f
        };

        return value * Math.Clamp(parameters.Depth, 0f, 1f);
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
