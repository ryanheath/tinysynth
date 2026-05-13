using TinySynth.Synth.AudioGraph;
using TinySynth.Synth.Modulation;
using TinySynth.Synth.Snapshots;

namespace TinySynth.Synth.Nodes;

internal sealed class ReverbNode(string name, int sampleRate, AudioNode inputNode) : AudioNode(name, inputNode)
{
    private readonly ReverbDelayLine[] _reverbLines =
        [
            new ReverbDelayLine(sampleRate, 0.097f),
            new ReverbDelayLine(sampleRate, 0.131f),
            new ReverbDelayLine(sampleRate, 0.173f),
            new ReverbDelayLine(sampleRate, 0.211f)
        ];

    protected override void Process(in AudioRenderContext context, IReadOnlyList<AudioNode> inputs, AudioBuffer output)
    {
        FxSnapshot parameters = GetEffectSnapshot(context.PatchSnapshot.Fx, context.GlobalModulationState);
        float[] inputSamples = inputs[0].Output.SampleArray;
        float[] outputSamples = output.SampleArray;

        for (int i = 0; i < inputSamples.Length; i += 2)
        {
            (float left, float right) = ProcessSample(inputSamples[i], inputSamples[i + 1], parameters);
            outputSamples[i] = left - inputSamples[i];
            outputSamples[i + 1] = right - inputSamples[i + 1];
        }
    }

    private (float Left, float Right) ProcessSample(float inputLeft, float inputRight, FxSnapshot parameters)
    {
        if (parameters.ReverbType == ReverbType.Off || parameters.ReverbMix <= 0.0001f)
        {
            return (inputLeft, inputRight);
        }

        (float sizeScale, float feedback, float brightness) = parameters.ReverbType switch
        {
            ReverbType.Room => (0.72f, 0.58f, 0.48f),
            ReverbType.Hall => (0.92f, 0.72f, 0.38f),
            ReverbType.Shimmer => (1.00f, 0.78f, 0.62f),
            _ => (0f, 0f, 0f)
        };

        float mix = Math.Clamp(parameters.ReverbMix, 0f, 1f);
        float size = Math.Clamp(parameters.ReverbSize, 0.10f, 1f);
        float damping = Math.Clamp(parameters.ReverbDamping, 0f, 1f);
        float toneResponse = Math.Clamp((1f - (damping * 0.85f)) * (0.55f + (brightness * 0.45f)), 0.05f, 0.95f);
        float effectiveFeedback = Math.Clamp(feedback + ((size - 0.5f) * 0.18f), 0.25f, 0.88f);
        float crossFeed = 0.10f;
        float wetLeftSum = 0f;
        float wetRightSum = 0f;

        for (int i = 0; i < _reverbLines.Length; i++)
        {
            ReverbDelayLine line = _reverbLines[i];
            float delaySamples = line.MaxDelaySamples * Math.Clamp((0.45f + (size * 0.55f)) * sizeScale, 0.20f, 1f);
            float delayedLeft = ReadDelay(line.LeftBuffer, line.WriteIndex, delaySamples);
            float delayedRight = ReadDelay(line.RightBuffer, line.WriteIndex, delaySamples);
            line.FilterStateLeft += (delayedLeft - line.FilterStateLeft) * toneResponse;
            line.FilterStateRight += (delayedRight - line.FilterStateRight) * toneResponse;
            float filteredLeft = line.FilterStateLeft;
            float filteredRight = line.FilterStateRight;
            float polarity = (i & 1) == 0 ? 1f : -1f;

            line.LeftBuffer[line.WriteIndex] = inputLeft + (filteredLeft * effectiveFeedback) + (filteredRight * crossFeed);
            line.RightBuffer[line.WriteIndex] = inputRight + (filteredRight * effectiveFeedback) + (filteredLeft * crossFeed);
            line.WriteIndex = (line.WriteIndex + 1) % line.Buffer.Length;
            wetLeftSum += filteredLeft * polarity;
            wetRightSum += filteredRight * -polarity;
        }

        float wetLeft = wetLeftSum * 0.35f;
        float wetRight = wetRightSum * 0.35f;
        return (
            (inputLeft * (1f - (mix * 0.55f))) + (wetLeft * mix),
            (inputRight * (1f - (mix * 0.55f))) + (wetRight * mix));
    }

    private static FxSnapshot GetEffectSnapshot(FxSnapshot source, GlobalModulationState modulationState)
    {
        return source with
        {
            ChorusMix = Math.Clamp(source.ChorusMix + modulationState.ChorusMix, 0f, 1f),
            DelayMix = Math.Clamp(source.DelayMix + modulationState.DelayMix, 0f, 1f),
            ReverbMix = Math.Clamp(source.ReverbMix + modulationState.ReverbMix, 0f, 1f)
        };
    }

    private static float ReadDelay(float[] buffer, int writeIndex, float delaySamples)
    {
        double readPosition = writeIndex - delaySamples;
        readPosition %= buffer.Length;

        if (readPosition < 0d)
        {
            readPosition += buffer.Length;
        }

        int sampleIndexA = (int)readPosition;

        if (sampleIndexA >= buffer.Length)
        {
            sampleIndexA = 0;
        }

        int sampleIndexB = (sampleIndexA + 1) % buffer.Length;
        float fraction = (float)(readPosition - sampleIndexA);
        return buffer[sampleIndexA] + ((buffer[sampleIndexB] - buffer[sampleIndexA]) * fraction);
    }

    private sealed class ReverbDelayLine
    {
        public ReverbDelayLine(int sampleRate, float maxDelaySeconds)
        {
            int bufferLength = Math.Max(64, (int)MathF.Ceiling(sampleRate * maxDelaySeconds));
            LeftBuffer = new float[bufferLength];
            RightBuffer = new float[bufferLength];
        }

        public float[] LeftBuffer { get; }

        public float[] RightBuffer { get; }

        public float[] Buffer => LeftBuffer;

        public float FilterStateLeft { get; set; }

        public float FilterStateRight { get; set; }

        public int MaxDelaySamples => Buffer.Length - 1;

        public int WriteIndex { get; set; }
    }
}
