using TinySynth.Synth.AudioGraph;
using TinySynth.Synth.Modulation;
using TinySynth.Synth.Snapshots;

namespace TinySynth.Synth.Nodes;

internal sealed class DelayNode : AudioNode
{
    private readonly int _sampleRate;
    private readonly float[][] _buffers;
    private readonly float[] _filterState = new float[2];
    private int _writeIndex;
    private float _modulationPhase;

    public DelayNode(string name, int sampleRate, AudioNode inputNode)
        : base(name, inputNode)
    {
        _sampleRate = sampleRate;
        int bufferLength = Math.Max(4096, sampleRate);
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
        if (parameters.DelayType == DelayType.Off || parameters.DelayMix <= 0.0001f)
        {
            return (inputLeft, inputRight);
        }

        (float timeScale, float feedbackScale, float toneResponse, float modulationDepth) = parameters.DelayType switch
        {
            DelayType.Slap => (0.45f, 0.55f, 0.72f, 0f),
            DelayType.PingPong => (0.85f, 0.70f, 0.58f, 0.0015f),
            DelayType.Tape => (1.00f, 0.78f, 0.42f, 0.0035f),
            _ => (0f, 0f, 1f, 0f)
        };

        float mix = Math.Clamp(parameters.DelayMix, 0f, 1f);
        float delaySeconds = Math.Clamp(parameters.DelayTimeSeconds * timeScale, 0.04f, 0.95f);
        float feedback = Math.Clamp(parameters.DelayFeedback * feedbackScale, 0f, 0.88f);
        float modulation = modulationDepth <= 0f
            ? 0f
            : MathF.Sin(_modulationPhase * MathF.Tau) * (_sampleRate * modulationDepth);

        if (modulationDepth > 0f)
        {
            _modulationPhase = AdvancePhase(_modulationPhase, Math.Max(parameters.ChorusRateHz, 0.05f) / _sampleRate);
        }

        float delaySamples = Math.Clamp((delaySeconds * _sampleRate) + modulation, 1f, _buffers[0].Length - 2f);
        float delayedLeft = ReadDelay(_buffers[0], _writeIndex, delaySamples);
        float delayedRight = ReadDelay(_buffers[1], _writeIndex, delaySamples);

        _filterState[0] += (delayedLeft - _filterState[0]) * toneResponse;
        _filterState[1] += (delayedRight - _filterState[1]) * toneResponse;
        float filteredLeft = _filterState[0];
        float filteredRight = _filterState[1];

        if (parameters.DelayType == DelayType.PingPong)
        {
            _buffers[0][_writeIndex] = inputLeft + (filteredRight * feedback);
            _buffers[1][_writeIndex] = inputRight + (filteredLeft * feedback);
        }
        else
        {
            _buffers[0][_writeIndex] = inputLeft + (filteredLeft * feedback);
            _buffers[1][_writeIndex] = inputRight + (filteredRight * feedback);
        }

        _writeIndex = (_writeIndex + 1) % _buffers[0].Length;

        return (
            (inputLeft * (1f - (mix * 0.45f))) + (filteredLeft * mix),
            (inputRight * (1f - (mix * 0.45f))) + (filteredRight * mix));
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
