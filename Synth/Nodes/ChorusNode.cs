using TinySynth.Synth.AudioGraph;
using TinySynth.Synth.Modulation;
using TinySynth.Synth.Snapshots;

namespace TinySynth.Synth.Nodes;

internal sealed class ChorusNode : AudioNode
{
    private readonly int _sampleRate;
    private readonly float[][] _buffers;
    private int _writeIndex;
    private float _phaseA;
    private float _phaseB;

    public ChorusNode(string name, int sampleRate, AudioNode inputNode)
        : base(name, inputNode)
    {
        _sampleRate = sampleRate;
        int bufferLength = Math.Max(2048, sampleRate / 2);
        _buffers =
        [
            new float[bufferLength],
            new float[bufferLength]
        ];
    }

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
        if (parameters.ChorusType == ChorusType.Off || parameters.ChorusMix <= 0.0001f)
        {
            return (inputLeft, inputRight);
        }

        (float baseDelayMs, float depthMs, float rateScale, float feedback) = parameters.ChorusType switch
        {
            ChorusType.Light => (11f, 4f, 1.0f, 0.08f),
            ChorusType.Ensemble => (16f, 7f, 1.15f, 0.12f),
            ChorusType.Wide => (21f, 10f, 0.85f, 0.18f),
            _ => (0f, 0f, 1f, 0f)
        };

        float rateHz = Math.Clamp(parameters.ChorusRateHz * rateScale, 0.05f, 4f);
        float depthScale = Math.Clamp(parameters.ChorusDepth, 0.05f, 1f);
        float mix = Math.Clamp(parameters.ChorusMix, 0f, 1f);
        float tremoloDepth = Math.Clamp(parameters.ChorusTremoloDepth, 0f, 1f);

        _phaseA = AdvancePhase(_phaseA, rateHz / _sampleRate);
        _phaseB = AdvancePhase(_phaseB, (rateHz * 1.31f) / _sampleRate);

        float delaySamplesA = ((baseDelayMs + (MathF.Sin(_phaseA * MathF.Tau) * depthMs * depthScale)) * _sampleRate) / 1000f;
        float delaySamplesB = ((baseDelayMs + (MathF.Sin((_phaseB * MathF.Tau) + 1.7f) * depthMs * depthScale)) * _sampleRate) / 1000f;
        delaySamplesA = Math.Clamp(delaySamplesA, 1f, _buffers[0].Length - 2f);
        delaySamplesB = Math.Clamp(delaySamplesB, 1f, _buffers[0].Length - 2f);

        float wetLeftA = ReadDelay(_buffers[0], _writeIndex, delaySamplesA);
        float wetLeftB = ReadDelay(_buffers[0], _writeIndex, delaySamplesB);
        float wetRightA = ReadDelay(_buffers[1], _writeIndex, delaySamplesB);
        float wetRightB = ReadDelay(_buffers[1], _writeIndex, delaySamplesA);
        float wetLeft = (wetLeftA * 0.7f) + (wetRightA * 0.3f);
        float wetRight = (wetRightB * 0.7f) + (wetLeftB * 0.3f);
        float tremolo = 1f - (((MathF.Sin((_phaseA * MathF.Tau) + 0.9f) + 1f) * 0.5f) * tremoloDepth);
        wetLeft *= tremolo;
        wetRight *= 1f - (((MathF.Sin((_phaseB * MathF.Tau) + 2.1f) + 1f) * 0.5f) * tremoloDepth);

        _buffers[0][_writeIndex] = inputLeft + (wetLeft * feedback);
        _buffers[1][_writeIndex] = inputRight + (wetRight * feedback);
        _writeIndex = (_writeIndex + 1) % _buffers[0].Length;

        return (
            (inputLeft * (1f - (mix * 0.45f))) + (wetLeft * mix),
            (inputRight * (1f - (mix * 0.45f))) + (wetRight * mix));
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

    private static float AdvancePhase(float phase, float increment)
    {
        phase += increment;
        phase -= MathF.Floor(phase);
        return phase;
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
}
