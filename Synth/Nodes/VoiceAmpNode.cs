using TinySynth.Synth.AudioGraph;

namespace TinySynth.Synth.Nodes;

internal sealed class VoiceAmpNode : AudioNode
{
    private readonly SynthVoice _voice;
    private readonly Action<SynthVoice, AudioRenderContext, AudioBuffer, AudioBuffer> _processor;

    public VoiceAmpNode(string name, SynthVoice voice, AudioNode inputNode, Action<SynthVoice, AudioRenderContext, AudioBuffer, AudioBuffer> processor)
        : base(name, inputNode)
    {
        _voice = voice;
        _processor = processor;
    }

    protected override void Process(in AudioRenderContext context, IReadOnlyList<AudioNode> inputs, AudioBuffer output)
    {
        _processor(_voice, context, inputs[0].Output, output);
    }
}
