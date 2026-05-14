using TinySynth.Synth.AudioGraph;

namespace TinySynth.Synth.Nodes;

internal sealed class VoiceMixerNode(string name, bool normalizeByVoiceCount, params AudioNode[] inputs) : AudioNode(name, inputs)
{
    private const float SilenceThreshold = 0.000001f;

    private readonly bool _normalizeByVoiceCount = normalizeByVoiceCount;

    protected override void Process(in AudioRenderContext context, IReadOnlyList<AudioNode> inputs, AudioBuffer output)
    {
        int activeInputCount = 0;
        Span<float> mixedSamples = output.Samples;

        foreach (AudioNode input in inputs)
        {
            if (!AddIfAudible(input.Output, mixedSamples))
            {
                continue;
            }

            activeInputCount++;
        }

        if (!_normalizeByVoiceCount || activeInputCount <= 1)
        {
            return;
        }

        float scale = 1f / MathF.Sqrt(activeInputCount);

        for (int i = 0; i < mixedSamples.Length; i++)
        {
            mixedSamples[i] *= scale;
        }
    }

    private static bool AddIfAudible(AudioBuffer buffer, Span<float> destination)
    {
        ReadOnlySpan<float> samples = buffer.ReadOnlySamples;
        int sampleCount = Math.Min(samples.Length, destination.Length);
        bool hasAudibleSample = false;

        for (int i = 0; i < sampleCount; i++)
        {
            float sample = samples[i];

            if (!hasAudibleSample && MathF.Abs(sample) > SilenceThreshold)
            {
                hasAudibleSample = true;
            }

            destination[i] += sample;
        }

        return hasAudibleSample;
    }
}
