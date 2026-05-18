using TinySynth.Synth.AudioGraph;

namespace TinySynth.Synth.Nodes;

internal sealed class VoiceMixerNode(string name, bool normalizeByVoiceCount, params AudioNode[] inputs) : AudioNode(name, inputs)
{
    private const float SilenceThreshold = 0.000001f;
    private const float SilenceEnergyThreshold = SilenceThreshold * SilenceThreshold;

    private readonly bool _normalizeByVoiceCount = normalizeByVoiceCount;

    protected override void Process(in AudioRenderContext context, IReadOnlyList<AudioNode> inputs, AudioBuffer output)
    {
        Span<float> mixedSamples = output.Samples;
        float totalEnergy = 0f;
        float loudestInputEnergy = 0f;

        foreach (AudioNode input in inputs)
        {
            float inputEnergy = AddAndMeasureEnergy(input.Output, mixedSamples);

            if (inputEnergy <= SilenceEnergyThreshold)
            {
                continue;
            }

            totalEnergy += inputEnergy;
            loudestInputEnergy = MathF.Max(loudestInputEnergy, inputEnergy);
        }

        if (!_normalizeByVoiceCount || totalEnergy <= 0f || loudestInputEnergy <= 0f)
        {
            return;
        }

        float scale = MathF.Sqrt(loudestInputEnergy / totalEnergy);

        if (scale >= 0.9999f)
        {
            return;
        }

        for (int i = 0; i < mixedSamples.Length; i++)
        {
            mixedSamples[i] *= scale;
        }
    }

    private static float AddAndMeasureEnergy(AudioBuffer buffer, Span<float> destination)
    {
        ReadOnlySpan<float> samples = buffer.ReadOnlySamples;
        int sampleCount = Math.Min(samples.Length, destination.Length);

        if (sampleCount == 0)
        {
            return 0f;
        }

        double sumSquares = 0d;

        for (int i = 0; i < sampleCount; i++)
        {
            float sample = samples[i];
            destination[i] += sample;
            sumSquares += sample * sample;
        }

        return (float)(sumSquares / sampleCount);
    }
}
