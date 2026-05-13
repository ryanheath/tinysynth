using TinySynth.Synth.AudioGraph;

namespace TinySynth.Synth.Nodes;

internal sealed class OutputNode(string name, float outputGain, AudioNode inputNode) : AudioNode(name, inputNode)
{
    private readonly float _outputGain = outputGain;

    protected override void Process(in AudioRenderContext context, IReadOnlyList<AudioNode> inputs, AudioBuffer output)
    {
        output.AddFrom(inputs[0].Output);
        Span<float> samples = output.Samples;

        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = MathF.Tanh(samples[i] * _outputGain);
        }
    }
}
