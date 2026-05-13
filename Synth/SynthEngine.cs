using TinySynth.Synth.AudioGraph;
using TinySynth.Synth.Modulation;
using TinySynth.Synth.Nodes;
using TinySynth.Synth.Snapshots;

namespace TinySynth.Synth;

internal sealed class SynthEngine
{
    private const int StereoChannelCount = 2;

    private readonly int _sampleRate;
    private readonly VoicePool _voicePool;
    private readonly HashSet<int> _activeNotes = [];
    private bool _holdPedalEnabled;
    private readonly AudioGraphScheduler _audioGraphScheduler;
    private readonly GlobalModulationRuntime _globalModulationRuntime;
    private readonly Dictionary<SynthVoice, VoiceOscNode> _voiceOscNodes = [];

    public SynthEngine(int sampleRate, float masterGain, int defaultMidiNote, int voiceCount)
    {
        _sampleRate = sampleRate;
        _voicePool = new VoicePool(sampleRate, masterGain, defaultMidiNote, voiceCount);
        _globalModulationRuntime = new GlobalModulationRuntime(sampleRate);
        _audioGraphScheduler = CreateAudioGraphScheduler();
        RefreshVoiceState();
    }

    public IReadOnlySet<int> ActiveNotes => _activeNotes;

    public int ActiveVoiceCount { get; private set; }

    private VoiceSlot? _displaySlot;

    public int DisplayMidiNote => _displaySlot?.Voice.ActiveMidiNote ?? -1;

    public float DisplayFrequency => _displaySlot?.Voice.CurrentFrequency ?? 0f;

    public EnvelopeStage DisplayEnvelopeStage => _displaySlot?.Voice.EnvelopeStage ?? EnvelopeStage.Idle;

    public void SetMasterGain(float masterGain)
    {
        _voicePool.SetMasterGain(masterGain);
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
            _voicePool.ReleaseUnheldVoices();
        }

        RefreshVoiceState();
    }

    public void NoteOn(int midiNote, SynthParameters parameters)
    {
        _voicePool.StartNote(midiNote, parameters);
        RefreshVoiceState();
    }

    public void NoteOff(int midiNote)
    {
        _voicePool.ReleaseNote(midiNote, _holdPedalEnabled);
        RefreshVoiceState();
    }

    public void RenderBlock(float[] audioBuffer, float[] scopeBuffer, ref int scopeWriteIndex, int blockId, SynthParameters parameters)
    {
        SynthPatchSnapshot patchSnapshot = SynthPatchSnapshot.Create(parameters);
        VoiceStateSnapshot voiceState = VoiceStateAggregator.Capture(_voicePool.Slots, _voiceOscNodes);
        GlobalModulationState globalModulationState = _globalModulationRuntime.AdvanceAndBuild(
            patchSnapshot,
            audioBuffer.Length / StereoChannelCount,
            voiceState.AverageEnvelopeLevel,
            voiceState.KeyTrackMidiNote);
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

    private void RefreshVoiceState()
    {
        VoiceStateSnapshot voiceState = VoiceStateAggregator.Capture(_voicePool.Slots, _voiceOscNodes);
        _activeNotes.Clear();
        foreach (int midiNote in voiceState.ActiveNotes)
        {
            _activeNotes.Add(midiNote);
        }

        ActiveVoiceCount = voiceState.ActiveVoiceCount;
        _displaySlot = voiceState.DisplaySlot;
    }

    private AudioGraphScheduler CreateAudioGraphScheduler()
    {
        AudioNode[] ampNodes = new AudioNode[_voicePool.Slots.Count];

        for (int i = 0; i < _voicePool.Slots.Count; i++)
        {
            SynthVoice voice = _voicePool.Slots[i].Voice;
            VoiceOscNode oscNode = new($"Voice{i + 1}.Osc", voice);
            _voiceOscNodes[voice] = oscNode;
            VoiceFilterNode filterNode = new($"Voice{i + 1}.Filter", voice, oscNode);
            ampNodes[i] = new VoiceAmpNode($"Voice{i + 1}.Amp", voice, filterNode);
        }

        VoiceMixerNode voiceBusNode = new("VoiceMixer", normalizeByVoiceCount: true, ampNodes);
        ChorusNode chorusNode = new("Chorus", _sampleRate, voiceBusNode);
        DelayNode delayNode = new("Delay", _sampleRate, voiceBusNode);
        ReverbNode reverbNode = new("Reverb", _sampleRate, voiceBusNode);
        MainMixerNode mainMixerNode = new("MainMixer", voiceBusNode, chorusNode, delayNode, reverbNode);
        OutputNode outputNode = new("Output", 1f, mainMixerNode);
        return new AudioGraphScheduler(new AudioGraph.AudioGraph(outputNode));
    }
}
