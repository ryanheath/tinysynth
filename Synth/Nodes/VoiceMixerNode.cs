using TinySynth.Synth.AudioGraph;

namespace TinySynth.Synth.Nodes;

internal sealed class VoiceMixerNode(string name, bool normalizeByVoiceCount, params AudioNode[] inputs) : AudioNode(name, inputs)
{
    private readonly bool _normalizeByVoiceCount = normalizeByVoiceCount;

    protected override void Process(in AudioRenderContext context, IReadOnlyList<AudioNode> inputs, AudioBuffer output)
    {
        int activeInputCount = 0;

        foreach (AudioNode input in inputs)
        {
            if (IsSilent(input.Output))
            {
                continue;
            }

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

    private static bool IsSilent(AudioBuffer buffer)
    {
        ReadOnlySpan<float> samples = buffer.ReadOnlySamples;

        for (int i = 0; i < samples.Length; i++)
        {
            if (MathF.Abs(samples[i]) > 0.000001f)
            {
                return false;
            }
        }

        return true;
    }
}
