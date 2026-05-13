using TinySynth.Synth.AudioGraph;

namespace TinySynth.Synth.Nodes;

internal sealed class VoiceOscNode : AudioNode
{
    private readonly SynthVoice _voice;
    private readonly Action<SynthVoice, AudioRenderContext, AudioBuffer> _renderer;

    public VoiceOscNode(string name, SynthVoice voice, Action<SynthVoice, AudioRenderContext, AudioBuffer> renderer)
        : base(name)
    {
        _voice = voice;
        _renderer = renderer;
    }

    protected override void Process(in AudioRenderContext context, IReadOnlyList<AudioNode> inputs, AudioBuffer output)
    {
        _renderer(_voice, context, output);
    }
}
