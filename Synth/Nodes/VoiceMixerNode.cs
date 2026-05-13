using TinySynth.Synth.AudioGraph;

namespace TinySynth.Synth.Nodes;

internal sealed class VoiceMixerNode : AudioNode
{
    private readonly bool _normalizeByVoiceCount;

    public VoiceMixerNode(string name, bool normalizeByVoiceCount, params AudioNode[] inputs)
        : base(name, inputs)
    {
        _normalizeByVoiceCount = normalizeByVoiceCount;
    }

    protected override void Process(in AudioRenderContext context, IReadOnlyList<AudioNode> inputs, AudioBuffer output)
    {
        int activeInputCount = 0;

        foreach (AudioNode input in inputs)
        {
            output.AddFrom(input.Output);
            activeInputCount++;
        }

        if (!_normalizeByVoiceCount || activeInputCount <= 1)
        {
            return;
        }

        float scale = 1f / MathF.Sqrt(activeInputCount);
        Span<float> samples = output.Samples;

        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] *= scale;
        }
    }
}
