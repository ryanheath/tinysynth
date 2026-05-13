using TinySynth.Synth.AudioGraph;

namespace TinySynth.Synth.Nodes;

internal sealed class MainMixerNode(string name, params AudioNode[] inputs) : AudioNode(name, inputs)
{
    protected override void Process(in AudioRenderContext context, IReadOnlyList<AudioNode> inputs, AudioBuffer output)
    {
        foreach (AudioNode input in inputs)
        {
            output.AddFrom(input.Output);
        }
    }
}
