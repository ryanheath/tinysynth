using TinySynth.Synth.AudioGraph;

namespace TinySynth.Synth.Nodes;

internal sealed class VoiceAmpNode(string name, SynthVoice voice, AudioNode inputNode) : AudioNode(name, inputNode)
{
    private readonly SynthVoice _voice = voice;

    protected override void Process(in AudioRenderContext context, IReadOnlyList<AudioNode> inputs, AudioBuffer output)
    {
        float[] inputSamples = inputs[0].Output.SampleArray;
        float[] outputSamples = output.SampleArray;
        float gain = _voice.MasterGain * _voice.NoteVelocity;

        for (int i = 0; i < inputSamples.Length; i++)
        {
            outputSamples[i] = inputSamples[i] * gain;
        }
    }
}
