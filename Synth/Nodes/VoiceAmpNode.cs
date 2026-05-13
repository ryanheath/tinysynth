using TinySynth.Synth.AudioGraph;

namespace TinySynth.Synth.Nodes;

internal sealed class VoiceAmpNode : AudioNode
{
    private readonly SynthVoice _voice;

    public VoiceAmpNode(string name, SynthVoice voice, AudioNode inputNode)
        : base(name, inputNode)
    {
        _voice = voice;
    }

    protected override void Process(in AudioRenderContext context, IReadOnlyList<AudioNode> inputs, AudioBuffer output)
    {
        float[] inputSamples = inputs[0].Output.SampleArray;
        float[] outputSamples = output.SampleArray;
        float gain = _voice.MasterGain;

        for (int i = 0; i < inputSamples.Length; i++)
        {
            outputSamples[i] = inputSamples[i] * gain;
        }
    }
}
