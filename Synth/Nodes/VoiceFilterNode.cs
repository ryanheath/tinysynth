using TinySynth.Synth.AudioGraph;

namespace TinySynth.Synth.Nodes;

internal sealed class VoiceFilterNode(string name, SynthVoice voice, AudioNode inputNode) : AudioNode(name, inputNode)
{
    private const int StereoChannelCount = 2;

    private readonly SynthVoice _voice = voice;

    protected override void Process(in AudioRenderContext context, IReadOnlyList<AudioNode> inputs, AudioBuffer output)
    {
        _voice.EnsureStageBuffers(context.FrameCount);
        float[] inputSamples = inputs[0].Output.SampleArray;
        float[] outputSamples = output.SampleArray;

        if (context.PatchSnapshot.FilterType == FilterType.Off)
        {
            Array.Copy(inputSamples, outputSamples, Math.Min(inputSamples.Length, outputSamples.Length));
            return;
        }

        float[] filterCutoffBuffer = _voice.FilterCutoffBuffer;
        float[] filterResonanceBuffer = _voice.FilterResonanceBuffer;
        float[] filterInput1 = _voice.FilterInput1;
        float[] filterInput2 = _voice.FilterInput2;
        float[] filterOutput1 = _voice.FilterOutput1;
        float[] filterOutput2 = _voice.FilterOutput2;

        for (int frameIndex = 0; frameIndex < context.FrameCount; frameIndex++)
        {
            int sampleIndex = frameIndex * StereoChannelCount;
            float cutoffHz = filterCutoffBuffer[frameIndex];
            float resonance = filterResonanceBuffer[frameIndex];
            outputSamples[sampleIndex] = ProcessFilterSample(inputSamples[sampleIndex], context.PatchSnapshot.FilterType, cutoffHz, resonance, channelIndex: 0, _voice.SampleRate, filterInput1, filterInput2, filterOutput1, filterOutput2);
            outputSamples[sampleIndex + 1] = ProcessFilterSample(inputSamples[sampleIndex + 1], context.PatchSnapshot.FilterType, cutoffHz, resonance, channelIndex: 1, _voice.SampleRate, filterInput1, filterInput2, filterOutput1, filterOutput2);
        }
    }

    private static float ProcessFilterSample(
        float inputSample,
        FilterType filterType,
        float cutoffHz,
        float resonance,
        int channelIndex,
        int sampleRate,
        float[] filterInput1,
        float[] filterInput2,
        float[] filterOutput1,
        float[] filterOutput2)
    {
        float q = 0.707f + ((8f - 0.707f) * resonance * resonance);
        float omega = MathF.Tau * cutoffHz / sampleRate;
        float sinOmega = MathF.Sin(omega);
        float cosOmega = MathF.Cos(omega);
        float alpha = sinOmega / (2f * q);

        float b0;
        float b1;
        float b2;
        float a0 = 1f + alpha;
        float a1 = -2f * cosOmega;
        float a2 = 1f - alpha;

        switch (filterType)
        {
            case FilterType.LowPass:
                b0 = (1f - cosOmega) * 0.5f;
                b1 = 1f - cosOmega;
                b2 = (1f - cosOmega) * 0.5f;
                break;

            case FilterType.HighPass:
                b0 = (1f + cosOmega) * 0.5f;
                b1 = -(1f + cosOmega);
                b2 = (1f + cosOmega) * 0.5f;
                break;

            case FilterType.BandPass:
                b0 = alpha;
                b1 = 0f;
                b2 = -alpha;
                break;

            default:
                return inputSample;
        }

        float normalizedB0 = b0 / a0;
        float normalizedB1 = b1 / a0;
        float normalizedB2 = b2 / a0;
        float normalizedA1 = a1 / a0;
        float normalizedA2 = a2 / a0;

        float outputSample = (normalizedB0 * inputSample)
            + (normalizedB1 * filterInput1[channelIndex])
            + (normalizedB2 * filterInput2[channelIndex])
            - (normalizedA1 * filterOutput1[channelIndex])
            - (normalizedA2 * filterOutput2[channelIndex]);

        filterInput2[channelIndex] = filterInput1[channelIndex];
        filterInput1[channelIndex] = inputSample;
        filterOutput2[channelIndex] = filterOutput1[channelIndex];
        filterOutput1[channelIndex] = outputSample;

        return outputSample;
    }
}
