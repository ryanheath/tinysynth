namespace TinySynth.Synth;

internal sealed class VoiceSlot(SynthVoice voice)
{
    public SynthVoice Voice { get; } = voice;

    public bool IsHeld { get; set; }

    public long LastStartOrder { get; set; }
}
