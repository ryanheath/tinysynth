using TinySynth.Synth.AudioGraph;
using TinySynth.Synth.Snapshots;

namespace TinySynth.Synth.Nodes;

internal sealed class RenderSourceNode : AudioNode
{
    private readonly IReadOnlyList<SynthVoice> _voices;

    public RenderSourceNode(string name, IReadOnlyList<SynthVoice> voices)
        : base(name)
    {
        _voices = voices;
    }

    protected override void Process(in AudioRenderContext context, IReadOnlyList<AudioNode> inputs, AudioBuffer output)
    {
        int mixedVoiceCount = 0;
        float[] outputBuffer = output.SampleArray;

        foreach (SynthVoice voice in _voices)
        {
            if (voice.IsIdle)
            {
                continue;
            }

            mixedVoiceCount++;
            voice.AddToBuffer(outputBuffer, context.PatchSnapshot);
        }

        if (mixedVoiceCount <= 1)
        {
            return;
        }

        float mixScale = 1f / MathF.Sqrt(mixedVoiceCount);

        for (int i = 0; i < outputBuffer.Length; i++)
        {
            outputBuffer[i] *= mixScale;
        }
    }
}
